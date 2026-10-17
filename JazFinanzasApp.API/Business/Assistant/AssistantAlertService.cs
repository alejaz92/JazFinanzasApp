using JazFinanzasApp.API.Business.DTO.Dashboard;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JazFinanzasApp.API.Business.Assistant
{
    public interface IAssistantAlertService
    {
        // Los avisos que el asistente le suma a la bandeja de pendientes (plan-devoluciones-llm.md, D8, D8b y D9):
        //  - las tres alertas por reglas, para todos los usuarios (no cuestan nada ni mandan datos a ningún lado);
        //  - los hallazgos activos y el aviso de revisión de calidad, solo para quien tiene el asistente habilitado.
        // Nunca lanza: Inicio tiene que seguir funcionando aunque algo de esto falle.
        Task<List<DashboardPendingItemDTO>> BuildAsync(int userId, DateTime today);
    }

    // Cuándo toca una revisión de calidad (D9): pasaron 30 días desde la última (90 si las dos últimas no
    // cambiaron la versión de instrucciones) y hay al menos una devolución nueva desde esa revisión.
    public static class AssistantQualityReviewRule
    {
        public const int NormalIntervalDays = 30;
        public const int SpacedIntervalDays = 90;

        public record Result(bool Due, int DaysSince, int NewReviews);

        // lastReviews: las revisiones registradas, de la más nueva a la más vieja. Sin ninguna, la referencia es
        // la primera devolución terminada (la revisión "al lanzar" la registra quien la hace, en la Fase 16).
        public static Result Evaluate(DateTime now, IReadOnlyList<AssistantQualityReview> lastReviews, DateTime? firstReadyAt, int newReviews)
        {
            var reference = lastReviews.Count > 0 ? lastReviews[0].ReviewedAt : firstReadyAt;
            if (reference == null)
                return new Result(false, 0, 0);

            var daysSince = (int)Math.Floor((now - reference.Value).TotalDays);
            var twoWithoutChange = lastReviews.Count >= 2
                && lastReviews[0].PromptVersionBefore == lastReviews[0].PromptVersionAfter
                && lastReviews[1].PromptVersionBefore == lastReviews[1].PromptVersionAfter;
            var interval = twoWithoutChange ? SpacedIntervalDays : NormalIntervalDays;

            return new Result(daysSince >= interval && newReviews >= 1, daysSince, newReviews);
        }
    }

    public class AssistantAlertService : IAssistantAlertService
    {
        public const string FindingKind = "AssistantFinding";
        public const string QualityReviewDueKind = "AssistantQualityReviewDue";

        private const string PesoAssetName = "Peso Argentino";

        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssistantUsageService _usage;
        private readonly IAssistantFindingService _findings;
        private readonly IAssistantReviewRepository _reviewRepository;
        private readonly AssistantAlertsOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AssistantAlertService> _logger;

        public AssistantAlertService(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAssistantUsageService usage,
            IAssistantFindingService findings,
            IAssistantReviewRepository reviewRepository,
            IOptions<AssistantAlertsOptions> options,
            TimeProvider timeProvider,
            ILogger<AssistantAlertService> logger)
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _usage = usage;
            _findings = findings;
            _reviewRepository = reviewRepository;
            _options = options.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<List<DashboardPendingItemDTO>> BuildAsync(int userId, DateTime today)
        {
            var items = new List<DashboardPendingItemDTO>();

            try
            {
                items.AddRange(await BuildRuleAlertsAsync(userId, today));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallaron las alertas por reglas del usuario {UserId}", userId);
            }

            try
            {
                var status = await _usage.GetStatusAsync(userId);
                if (status.Enabled)
                {
                    items.AddRange(await BuildFindingItemsAsync(userId));
                    var due = await BuildQualityReviewDueAsync(userId);
                    if (due != null) items.Add(due);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallaron los avisos del asistente del usuario {UserId}", userId);
            }

            return items;
        }

        private async Task<List<DashboardPendingItemDTO>> BuildRuleAlertsAsync(int userId, DateTime today)
        {
            var pesos = await _assetRepository.GetAssetByNameAsync(PesoAssetName);
            if (pesos == null) return new List<DashboardPendingItemDTO>();

            var from = new DateTime(today.Year, today.Month, 1).AddMonths(-_options.RequiredHistoryMonths);
            var movements = (await _transactionRepository.GetAlertMovementsAsync(userId, pesos, from)).ToList();
            return AssistantAlertRules.Evaluate(movements, today, pesos.Symbol, _options);
        }

        // D8b: título y detalle son los del hallazgo; el link es la categoría, si la tiene. Desde la bandeja se puede
        // descartar y calificar, para lo cual el frontend necesita el id del hallazgo.
        private async Task<List<DashboardPendingItemDTO>> BuildFindingItemsAsync(int userId)
        {
            var findings = await _findings.GetAsync(userId, includeAll: false);
            return findings.Select(f => new DashboardPendingItemDTO
            {
                Kind = FindingKind,
                Title = f.Title,
                Detail = f.Detail,
                Severity = "info",
                LinkId = f.TransactionClassId,
                FindingId = f.Id,
                Date = f.CreatedAt
            }).ToList();
        }

        private async Task<DashboardPendingItemDTO?> BuildQualityReviewDueAsync(int userId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var lastReviews = await _reviewRepository.GetLastQualityReviewsAsync(userId, 2);
            var firstReady = await _reviewRepository.GetFirstReadyReviewTimeAsync(userId);

            var since = lastReviews.Count > 0 ? lastReviews[0].ReviewedAt : firstReady?.AddTicks(-1);
            var newReviews = since == null ? 0 : await _reviewRepository.CountReadyReviewsSinceAsync(userId, since.Value);

            var result = AssistantQualityReviewRule.Evaluate(now, lastReviews, firstReady, newReviews);
            if (!result.Due) return null;

            return new DashboardPendingItemDTO
            {
                Kind = QualityReviewDueKind,
                Title = "Revisión de calidad del asistente",
                Detail = $"Pasaron {result.DaysSince} días desde la última revisión y hay {result.NewReviews} " +
                         (result.NewReviews == 1 ? "devolución nueva." : "devoluciones nuevas."),
                Severity = "info"
            };
        }
    }
}
