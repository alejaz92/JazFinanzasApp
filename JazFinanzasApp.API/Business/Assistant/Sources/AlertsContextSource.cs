using JazFinanzasApp.API.Business.DTO.Dashboard;
using JazFinanzasApp.API.Business.Interfaces;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Alertas y pendientes (plan-devoluciones-llm.md, D2): la bandeja de pendientes de Inicio tal como está
    // hoy, que es la misma que ve el usuario. Hoy trae lo que la app ya avisa (tarjetas por vencer, deudas,
    // viajes sin gastos recientes, cobros de bonos, cotizaciones viejas); las alertas nuevas del asistente
    // (Fase 9) y sus hallazgos entran a la bandeja y por lo tanto a esta fuente sin tocarla.
    public class AlertsContextSource : IAssistantContextSource
    {
        public const string SourceKey = "alerts";

        private readonly IDashboardService _dashboard;
        private readonly IAssistantCurrencyResolver _currencies;

        public AlertsContextSource(IDashboardService dashboard, IAssistantCurrencyResolver currencies)
        {
            _dashboard = dashboard;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Los avisos y pendientes que la app muestra hoy en Inicio, al día de hoy y no al cierre de ningún mes. Cada monto va en la moneda del aviso, sin " +
            "convertir. 'severity' es info, warning o danger. Si un aviso ya figura en otra fuente (una tarjeta por vencer, un cobro de bono), es la misma " +
            "cosa vista desde la bandeja: no es un segundo hecho.";

        public string PrimaryCurrency => "ARS";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            // La bandeja es el estado de hoy: para un cierre viejo hablaría de hoy y no de ese mes.
            if (request.IsMonthClose && !request.IsLive)
                return null;

            var currencies = await _currencies.GetAsync();
            var dashboard = await _dashboard.GetDashboardAsync(request.UserId, currencies.Ars.Id);

            var items = dashboard.Pending.Select(ToEntry).ToList();
            return items.Count == 0 ? null : new AlertsData(DateTime.Today.ToString("yyyy-MM-dd"), items);
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
