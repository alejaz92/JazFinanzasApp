namespace JazFinanzasApp.API.Business.DTO.BondCollection
{
    // Historial de cobros registrados/descartados (cualquier Status) para la pestaña "Registrados"
    // de Cobros de Bonos.
    public class BondCollectionListDTO
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; } = "";
        public string Symbol { get; set; } = "";
        public DateTime PaymentDate { get; set; }
        public string AccountName { get; set; } = "";
        public string PortfolioName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? CollectionDate { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public string CurrencySymbol { get; set; } = "";
    }
}
