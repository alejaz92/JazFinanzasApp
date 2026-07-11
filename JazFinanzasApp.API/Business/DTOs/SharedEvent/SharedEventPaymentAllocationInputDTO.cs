namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    // Exactamente uno de los dos: SplitId (ítem a favor) o ShareId (deuda propia)
    public class SharedEventPaymentAllocationInputDTO
    {
        public int? SplitId { get; set; }
        public int? ShareId { get; set; }
        public decimal Amount { get; set; }
    }
}
