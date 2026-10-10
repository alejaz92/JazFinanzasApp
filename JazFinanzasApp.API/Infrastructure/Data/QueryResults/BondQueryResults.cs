namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    // Tenencia de un bono en una cuenta y cartera puntual, antes de una fecha de corte — una fila
    // por (cuenta, cartera) con tenencia viva (plan-amortizaciones-bonos.md, T4).
    // Un movimiento de un bono del usuario, con lo mínimo para calcular su tenencia a cualquier fecha
    // sin volver a la base (GetBondMovementsAsync).
    public class BondMovementResult
    {
        public int AssetId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; } = "";
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    public class BondHoldingResult
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; } = "";
        public decimal Quantity { get; set; }
    }
}
