namespace JazFinanzasApp.API.Business.DTO.CardTransactionDiscount
{
    public class CardTransactionDiscountDetailDTO
    {
        public int Id { get; set; }
        public int CardTransactionId { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountApplied { get; set; }

        // Cuanto del descuento ya salio de la tarjeta hacia una cuenta.
        public decimal AmountMaterialized { get; set; }

        // Lo que todavia esta como saldo a favor en la tarjeta: Amount - AmountMaterialized.
        public decimal PendingOnCard { get; set; }

        public string CreditTarget { get; set; } = string.Empty;
        public DateTime CreditDate { get; set; }
        public string? Notes { get; set; }
        public List<CardTransactionDiscountInstallmentDTO> Installments { get; set; } = new();
    }
}
