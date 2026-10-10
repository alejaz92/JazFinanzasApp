namespace JazFinanzasApp.API.Business.DTO.InvestmentReport
{
    // Aportes vs rendimiento (Flujo 5): cascada valor al inicio → aportes → retiros → valorización →
    // valor al final. Arranca en StartMonth (marzo de 2024, 1.5 del plan) — Valuation es el "plug"
    // (lo que no explican ni los aportes ni los retiros, es decir la ganancia o pérdida de precio).
    //
    // plan-amortizaciones-bonos, T12: Withdrawn incluye lo cobrado de bonos (capital + interés: sale de
    // lo invertido hacia una cuenta, igual que una venta), y por eso Valuation incluye los intereses
    // cobrados. InterestCollected permite separarlos: la valorización "pura" es Valuation -
    // InterestCollected, y la cascada cierra así: InitialValue + Contributed - Withdrawn +
    // (Valuation - InterestCollected) + InterestCollected = FinalValue.
    public class ContributionsVsPerformanceDTO
    {
        public string ReferenceAssetSymbol { get; set; }
        public DateTime StartMonth { get; set; }
        public decimal InitialValue { get; set; }
        public decimal Contributed { get; set; }
        public decimal Withdrawn { get; set; }
        public decimal Valuation { get; set; }
        public decimal InterestCollected { get; set; }
        public decimal FinalValue { get; set; }
    }
}
