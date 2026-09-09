namespace JazFinanzasApp.API.Business.DTO.InvestmentReport
{
    // Aportes vs rendimiento (Flujo 5): cascada valor al inicio → aportes → retiros → valorización →
    // valor al final. Arranca en StartMonth (marzo de 2024, 1.5 del plan) — Valuation es el "plug"
    // (lo que no explican ni los aportes ni los retiros, es decir la ganancia o pérdida de precio).
    public class ContributionsVsPerformanceDTO
    {
        public string ReferenceAssetSymbol { get; set; }
        public DateTime StartMonth { get; set; }
        public decimal InitialValue { get; set; }
        public decimal Contributed { get; set; }
        public decimal Withdrawn { get; set; }
        public decimal Valuation { get; set; }
        public decimal FinalValue { get; set; }
    }
}
