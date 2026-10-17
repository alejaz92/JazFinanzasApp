using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Implementación de IAssistantModelClient con el SDK oficial de Anthropic (plan-devoluciones-llm.md, D4).
    //
    // Autenticación: si Anthropic:ApiKey está cargada se usa esa clave (App Service en producción, variable
    // de entorno en local). Si no, el SDK arma el cliente con su flujo por defecto, que toma la federación
    // de identidades de las variables ANTHROPIC_* si están completas. Así pasar a federación más adelante no
    // toca código. El cliente se crea en el primer uso y no al arrancar: sin clave el backend levanta igual y
    // el asistente responde un error claro (Fase 14).
    public class AnthropicAssistantModelClient : IAssistantModelClient
    {
        public const string ApiKeySetting = "Anthropic:ApiKey";

        // Respaldo del lado del servidor ante una negativa del modelo, con su beta (D4).
        private const string ServerSideFallbackBeta = "server-side-fallback-2026-07-01";

        private readonly AssistantOptions _options;
        private readonly string? _apiKey;
        private readonly ILogger<AnthropicAssistantModelClient> _logger;
        private readonly Lazy<AnthropicClient> _client;

        public AnthropicAssistantModelClient(
            IOptions<AssistantOptions> options,
            IConfiguration configuration,
            ILogger<AnthropicAssistantModelClient> logger)
        {
            _options = options.Value;
            _apiKey = configuration[ApiKeySetting];
            _logger = logger;
            _client = new Lazy<AnthropicClient>(CreateClient, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        // Clave configurada, o las variables que el SDK usa por su cuenta (clave, token o federación).
        private bool HasCredentials() =>
            !string.IsNullOrWhiteSpace(_apiKey)
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_FEDERATION_RULE_ID"));

        private AnthropicClient CreateClient() =>
            string.IsNullOrWhiteSpace(_apiKey) ? new AnthropicClient() : new AnthropicClient { ApiKey = _apiKey };

        public async Task<AssistantModelResponse> GenerateAsync(AssistantModelRequest request, CancellationToken cancellationToken = default)
        {
            // El SDK arma el cliente sin credenciales y recién falla al llamar con un error poco claro: se
            // chequea antes para dar el mensaje correcto.
            if (!HasCredentials())
            {
                _logger.LogError("Falta {Setting} (o la federación de identidades): el asistente no puede llamar al modelo", ApiKeySetting);
                throw new AssistantModelException("El asistente no está configurado todavía: falta la clave de acceso al modelo.");
            }

            var client = _client.Value;

            try
            {
                var message = await client.Beta.Messages.Create(BuildParameters(request), cancellationToken: cancellationToken);
                return ToResponse(message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not AssistantModelException)
            {
                // El 400 por límite de gasto o saldo es lo único que se distingue; el resto es una falla genérica.
                if (ex is AnthropicBadRequestException badRequest)
                {
                    var providerText = $"{badRequest.Message} {badRequest.ResponseBody}";
                    if (AssistantSpendLimitDetector.IsSpendLimitMessage(providerText))
                        throw new AssistantSpendLimitException(providerText);
                }

                _logger.LogError(ex, "Falló la llamada a Anthropic");
                throw new AssistantModelException(inner: ex);
            }
        }

        // No va thinking: en Opus 5.5 el razonamiento adaptativo no se puede apagar y omitirlo equivale a
        // pedirlo adaptativo. No va caché de prompts: las instrucciones (~2.000 tokens) no llegan al mínimo
        // cacheable del modelo y los pedidos se separan por días, mucho más que los 5 minutos del caché
        // (Fase 5). El cálculo de costo igual lo contempla por si se activa.
        private MessageCreateParams BuildParameters(AssistantModelRequest request) => new()
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Betas = [ServerSideFallbackBeta],
            Fallbacks = new Default(),
            System = request.SystemPrompt,
            Messages = [new() { Role = Role.User, Content = request.UserContent }],
            OutputConfig = new BetaOutputConfig
            {
                Effort = ParseEffort(request.Effort),
                Format = new BetaJsonOutputFormat { Schema = ParseSchema(request.OutputSchemaJson) }
            }
        };

        private static Effort ParseEffort(string effort) => effort switch
        {
            AssistantEffort.High => Effort.High,
            AssistantEffort.Medium => Effort.Medium,
            _ => throw new ArgumentException($"Esfuerzo desconocido: {effort}", nameof(effort))
        };

        private static IReadOnlyDictionary<string, JsonElement> ParseSchema(string schemaJson) =>
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(schemaJson)
            ?? throw new ArgumentException("El esquema de salida está vacío.", nameof(schemaJson));

        // Model y StopReason son enums del SDK: ToString() devuelve el valor con comillas JSON ("\"end_turn\""),
        // así que se lee el texto crudo con Raw(). Lo encontró la primera llamada real (Fase 7): con las comillas,
        // la detección de negativas y cortes y la búsqueda de precios no habrían funcionado.
        public static AssistantModelResponse ToResponse(BetaMessage message)
        {
            var text = string.Concat(message.Content
                .Select(block => block.TryPickText(out BetaTextBlock? t) ? t.Text : null)
                .Where(t => t is not null));

            var usage = new AssistantModelUsage(
                message.Usage.InputTokens,
                message.Usage.OutputTokens,
                message.Usage.CacheReadInputTokens ?? 0,
                message.Usage.CacheCreationInputTokens ?? 0);

            return new AssistantModelResponse(
                message.Model.Raw(),
                string.IsNullOrEmpty(text) ? null : text,
                message.StopReason?.Raw(),
                usage);
        }
    }
}
