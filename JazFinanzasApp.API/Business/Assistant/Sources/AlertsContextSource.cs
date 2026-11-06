using JazFinanzasApp.API.Business.DTO.Dashboard;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Alertas y pendientes (plan-devoluciones-llm.md, D2): la bandeja de pendientes de Inicio tal como está
    // hoy, que es la misma que ve el usuario. Trae lo que la app ya avisa (tarjetas por vencer, deudas, viajes
    // sin gastos recientes, cobros de bonos, cotizaciones viejas), más las alertas por reglas y los hallazgos
    // activos del asistente (Fase 10). Entran a la bandeja y por lo tanto a esta fuente sin tocarla, y así cada
    // pasada ve lo que ya está avisado y no lo repite (D15, primer resguardo). El aviso de revisión de calidad
    // no viaja: es una tarea para el usuario y no le dice nada al modelo.
    public class AlertsContextSource : IAssistantContextSource
    {
        public const string SourceKey = "alerts";

        private readonly IDashboardService _dashboard;
        private readonly IAssistantFindingService _findings;
        private readonly IAssistantCurrencyResolver _currencies;

        public AlertsContextSource(IDashboardService dashboard, IAssistantFindingService findings, IAssistantCurrencyResolver currencies)
        {
            _dashboard = dashboard;
            _findings = findings;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Los avisos y pendientes que la app muestra hoy en Inicio, al día de hoy y no al cierre de ningún mes. Cada monto va en la moneda del aviso, sin " +
            "convertir. 'severity' es info, warning o danger. Incluye las alertas de la app por reglas fijas (CategorySpike: una categoría muy por encima de su promedio del mes; LargeMovement: un egreso mucho mayor que cualquiera previo de su categoría; MissingIncome: un ingreso habitual que todavía no llegó) y los hallazgos que el asistente ya dejó activos (AssistantFinding): lo que figura ahí ya está avisado y no hay que repetirlo. Si un aviso ya figura en otra fuente (una tarjeta por vencer, un cobro de bono), es la misma " +
            "cosa vista desde la bandeja: no es un segundo hecho.";

        public string PrimaryCurrency => "ARS";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            // La bandeja es el estado de hoy: para un cierre viejo hablaría de hoy y no de ese mes.
            if (request.IsMonthClose && !request.IsLive)
                return null;

            var currencies = await _currencies.GetAsync();
            var dashboard = await _dashboard.GetDashboardAsync(request.UserId, currencies.Ars.Id);

            // Los hallazgos activos se piden directamente y no a través de la bandeja (plan-devoluciones-llm-v2.md, D9): cuando los
            // hallazgos salgan de la bandeja de Inicio, el asistente tiene que seguir viéndolos para no repetirlos (D15 del v1). Si alguno
            // llegara también por la bandeja, se descarta acá para que no viaje dos veces.
            var items = dashboard.Pending
                .Where(p => p.Kind != AssistantAlertService.QualityReviewDueKind && p.Kind != AssistantAlertService.FindingKind)
                .Select(ToEntry)
                .ToList();
            items.AddRange(await LoadActiveFindingsAsync(request.UserId));
            return items.Count == 0 ? null : new AlertsData(DateTime.Today.ToString("yyyy-MM-dd"), items);
        }

        private async Task<IEnumerable<PendingEntry>> LoadActiveFindingsAsync(int userId)
        {
            try
            {
                return (await _findings.GetAsync(userId, includeAll: false)).Select(f => new PendingEntry(
                    AssistantAlertService.FindingKind,
                    f.Title,
                    AssistantFigures.ShortenDetail(f.Detail, 120),
                    null,
                    null,
                    f.CreatedAt.ToString("yyyy-MM-dd"),
                    "info")).ToList();
            }
            catch (BusinessRuleException)
            {
                // Sin el asistente habilitado no hay hallazgos que mostrar.
                return Array.Empty<PendingEntry>();
            }
        }

        // ── Pura: testeable sin mocks ─────────────────────────────────────────────────────────

        public static PendingEntry ToEntry(DashboardPendingItemDTO item) => new(
            item.Kind,
            item.Title,
            item.Detail == null ? null : AssistantFigures.ShortenDetail(item.Detail, 120),
            item.Amount.HasValue ? AssistantFigures.RoundMoney(item.Amount.Value, isPesos: item.AssetSymbol == "ARS") : null,
            item.AssetSymbol,
            item.Date?.ToString("yyyy-MM-dd"),
            item.Severity);

        public record PendingEntry(string Kind, string Title, string? Detail, decimal? Amount, string? AssetSymbol, string? Date, string Severity);

        public record AlertsData(string AsOf, IReadOnlyList<PendingEntry> Pending);
    }
}
