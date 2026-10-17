using System.Globalization;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Una llamada al modelo con lo que hace falta para registrarla (D11). ReviewId es nulable: una
    // llamada puede fallar antes de que exista la fila de la pasada.
    public record AssistantModelCall(int UserId, string Purpose, string PromptVersion, int? ReviewId, AssistantModelRequest Request);

    // Estado del gasto del mes. El mes es el de UTC, igual que el límite de gasto de la consola de Anthropic.
    public record AssistantUsageStatus(bool Enabled, decimal MonthlyBudgetUsd, decimal SpentUsd, DateTime RenewsOn)
    {
        // Pasado este porcentaje del tope no corre la revisión semanal: las devoluciones tienen prioridad (D11).
        public const decimal WeeklyScanThreshold = 0.8m;

        public bool BudgetReached => Enabled && SpentUsd >= MonthlyBudgetUsd;
        public bool WeeklyScanAllowed => Enabled && SpentUsd < MonthlyBudgetUsd * WeeklyScanThreshold;
    }

    public interface IAssistantUsageService
    {
        // BusinessRuleException si el usuario no tiene el asistente habilitado (D12).
        Task EnsureEnabledAsync(int userId);

        // No exige habilitación: lo usa GET status para decidir qué mostrar.
        Task<AssistantUsageStatus> GetStatusAsync(int userId);

        // Habilitación, tope, llamada y registro de gasto, en ese orden. Registra la llamada salga bien o
        // mal, porque las fallidas también se pagan, y lanza AssistantBudgetReachedException al tope.
        Task<AssistantModelResponse> CallModelAsync(AssistantModelCall call, CancellationToken cancellationToken = default);
    }

    public class AssistantUsageService : IAssistantUsageService
    {
        public const string NotEnabledMessage = "El asistente no está habilitado para tu usuario";

        private static readonly CultureInfo Spanish = new("es-AR");

        private readonly IAssistantUsageRepository _repository;
        private readonly IAssistantModelClient _modelClient;
        private readonly AssistantOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AssistantUsageService> _logger;

        public AssistantUsageService(
            IAssistantUsageRepository repository,
            IAssistantModelClient modelClient,
            IOptions<AssistantOptions> options,
            TimeProvider timeProvider,
            ILogger<AssistantUsageService> logger)
        {
            _repository = repository;
            _modelClient = modelClient;
            _options = options.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task EnsureEnabledAsync(int userId)
        {
            var settings = await _repository.GetSettingsAsync(userId);
            if (settings is null || !settings.Enabled)
                throw new BusinessRuleException(NotEnabledMessage);
        }

        public async Task<AssistantUsageStatus> GetStatusAsync(int userId)
        {
            var settings = await _repository.GetSettingsAsync(userId);
            var (monthStart, nextMonthStart) = CurrentMonth();
            if (settings is null || !settings.Enabled)
                return new AssistantUsageStatus(false, 0m, 0m, nextMonthStart);

            var spent = await _repository.SumCostExcludingEvalAsync(userId, monthStart, nextMonthStart);
            return new AssistantUsageStatus(true, settings.MonthlyBudgetUsd, spent, nextMonthStart);
        }

        // Costo en dólares con los tokens que devuelve la API y la tabla de precios por millón de tokens.
        public static decimal CalculateCost(ModelPrice price, AssistantModelUsage usage)
        {
            var cost = (usage.InputTokens * price.InputPerMillion
                        + usage.OutputTokens * price.OutputPerMillion
                        + usage.CacheReadTokens * price.CacheReadPerMillion
                        + usage.CacheWriteTokens * price.CacheWritePerMillion) / 1_000_000m;
            return Math.Round(cost, 6);
        }

        public async Task<AssistantModelResponse> CallModelAsync(AssistantModelCall call, CancellationToken cancellationToken = default)
        {
            if (!AssistantApiCallPurpose.IsValid(call.Purpose))
                throw new ArgumentException($"Propósito de llamada desconocido: {call.Purpose}", nameof(call));

            await EnsureEnabledAsync(call.UserId);

            // Sin precio no hay costo que registrar: se falla antes de gastar, no después con un costo en cero.
            var configuredPrice = _options.GetPrice(_options.Model)
                ?? throw new InvalidOperationException($"Falta el precio del modelo '{_options.Model}' en Assistant:Prices.");

            var status = await GetStatusAsync(call.UserId);
            if (call.Purpose != AssistantApiCallPurpose.Eval)
            {
                if (status.BudgetReached)
                    throw new AssistantBudgetReachedException(BudgetReachedMessage(status));
                if (call.Purpose == AssistantApiCallPurpose.WeeklyScan && !status.WeeklyScanAllowed)
                    throw new AssistantBudgetReachedException(
                        $"La revisión semanal se saltea: ya se usó más del 80 % del tope mensual del asistente. {BudgetReachedMessage(status)}");
            }

            AssistantModelResponse response;
            try
            {
                response = await _modelClient.GenerateAsync(call.Request, cancellationToken);
            }
            catch (AssistantSpendLimitException ex)
            {
                // El límite de Anthropic cortó antes que el de la app. No se cobró nada, pero queda registrado.
                _logger.LogWarning(ex, "Anthropic rechazó el pedido por límite de gasto o saldo");
                await RecordAsync(call, _options.Model, AssistantModelUsage.None, 0m, "spend_limit", succeeded: false);
                throw new AssistantBudgetReachedException(BudgetReachedMessage(status));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await RecordAsync(call, _options.Model, AssistantModelUsage.None, 0m, "error", succeeded: false);
                throw;
            }

            // Si respondió el modelo de respaldo y no tiene precio cargado, se usa el del configurado y se avisa.
            var price = _options.GetPrice(response.Model);
            if (price is null)
            {
                _logger.LogWarning("Sin precio para el modelo {Model}: se usa el de {Configured}", response.Model, _options.Model);
                price = configuredPrice;
            }

            var cost = CalculateCost(price, response.Usage);
            await RecordAsync(call, response.Model, response.Usage, cost, response.StopReason, succeeded: response.IsUsable);
            return response;
        }

        private async Task RecordAsync(AssistantModelCall call, string model, AssistantModelUsage usage, decimal cost, string? stopReason, bool succeeded)
        {
            await _repository.AddApiCallAsync(new AssistantApiCall
            {
                UserId = call.UserId,
                Purpose = call.Purpose,
                Model = model,
                PromptVersion = call.PromptVersion,
                InputTokens = usage.InputTokens,
                OutputTokens = usage.OutputTokens,
                CacheReadTokens = usage.CacheReadTokens,
                CacheWriteTokens = usage.CacheWriteTokens,
                CostUsd = cost,
                StopReason = stopReason,
                Succeeded = succeeded,
                ReviewId = call.ReviewId
            });
        }

        private (DateTime MonthStart, DateTime NextMonthStart) CurrentMonth()
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return (start, start.AddMonths(1));
        }

        // El mismo mensaje para el tope propio y para el de Anthropic: dice cuánto se gastó y cuándo se renueva.
        private static string BudgetReachedMessage(AssistantUsageStatus status) =>
            $"Se alcanzó el tope mensual de gasto del asistente (US$ {status.SpentUsd.ToString("0.00", Spanish)} de US$ {status.MonthlyBudgetUsd.ToString("0.00", Spanish)}). " +
            $"Vuelve a funcionar el {status.RenewsOn.ToString("dd/MM/yyyy", Spanish)}.";
    }
}
