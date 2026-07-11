namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventPaymentPreviewDTO
    {
        public decimal CreditsAllocated { get; set; }
        public decimal DebtsAllocated { get; set; }
        public List<SharedEventPaymentPreviewItemDTO> Items { get; set; } = new();
    }
}
