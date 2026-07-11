namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventPaymentAllocationDTO
    {
        public int Id { get; set; }
        public int? SplitId { get; set; }
        public int? ShareId { get; set; }
        public decimal Amount { get; set; }
    }
}
