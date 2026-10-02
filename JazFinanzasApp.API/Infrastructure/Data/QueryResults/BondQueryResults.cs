namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    // Tenencia de un bono en una cuenta y cartera puntual, antes de una fecha de corte — una fila
    // por (cuenta, cartera) con tenencia viva (plan-amortizaciones-bonos.md, T4).
    public class BondHoldingResult
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; } = "";
        public decimal Quantity { get; set; }
    }
}
