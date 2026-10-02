namespace JazFinanzasApp.API.Business.DTO.BondCollection
{
    // Un pago de bono pendiente de registrar para una tenencia puntual (plan-amortizaciones-bonos.md,
    // T9). EstimatedCapital/EstimatedInterest vienen null si el bono es indexado (T6) — el formulario
    // no propone importe y el usuario carga capital e interés a mano.
    public class BondCollectionPendingDTO
    {
        public int BondPaymentId { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; } = "";
        public string Symbol { get; set; } = "";
        public DateTime PaymentDate { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; } = "";
        public decimal HeldQuantity { get; set; }
        public string CurrencySymbol { get; set; } = "";
        public bool IsIndexed { get; set; }
        public decimal? EstimatedCapital { get; set; }
        public decimal? EstimatedInterest { get; set; }
    }
}
