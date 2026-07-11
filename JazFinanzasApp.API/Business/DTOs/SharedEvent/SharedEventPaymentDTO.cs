namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventPaymentDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public decimal Amount { get; set; }

        public int? FromPersonId { get; set; }
        public string? FromPersonName { get; set; }
        public int? ToPersonId { get; set; }
        public string? ToPersonName { get; set; }

        public int? AccountId { get; set; }
        public bool IsInternalCompensation { get; set; }
        public string? Notes { get; set; }

        public List<SharedEventPaymentAllocationDTO> Allocations { get; set; } = new();
    }
}
