namespace JazFinanzasApp.API.Business.DTO.CardTransactionDiscount
{
    public class CardTransactionDiscountDetailDTO
    {
        public int Id { get; set; }
        public int CardTransactionId { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountApplied { get; set; }
        public string? Notes { get; set; }
    }
}
