namespace JazFinanzasApp.API.Business.DTO.BondCollection
{
    public class BondCollectionRegisterDTO
    {
        public int BondPaymentId { get; set; }
        public int AccountId { get; set; }
        public int PortfolioId { get; set; }
        public DateTime CollectionDate { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal InterestAmount { get; set; }

        // Requerido solo si InterestAmount > 0 (T7: categoría de ingreso elegida por el usuario,
        // precargada en el frontend con GetLastInterestClassIdAsync).
        public int? InterestTransactionClassId { get; set; }
    }
}
