namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    // Exactamente uno de los dos modos: por cuenta (AccountId) o por tarjeta (CardId + Installments + FirstInstallment)
    public class SharedEventMovementPaymentInputDTO
    {
        public int? AccountId { get; set; }
        public int? CardId { get; set; }
        public int? Installments { get; set; }
        public DateTime? FirstInstallment { get; set; }
    }
}
