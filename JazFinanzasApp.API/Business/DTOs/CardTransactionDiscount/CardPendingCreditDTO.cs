namespace JazFinanzasApp.API.Business.DTO.CardTransactionDiscount
{
    // Saldo a favor todavia pendiente en una tarjeta, con el detalle de que compras lo generaron.
    public class CardPendingCreditDTO
    {
        public int CardId { get; set; }
        public decimal TotalPending { get; set; }
        public List<CardPendingCreditItemDTO> Items { get; set; } = new();
    }

    public class CardPendingCreditItemDTO
    {
        public int DiscountId { get; set; }
        public int CardTransactionId { get; set; }
        public string? Detail { get; set; }
        public DateTime CreditDate { get; set; }
        public decimal Pending { get; set; }
    }
}
