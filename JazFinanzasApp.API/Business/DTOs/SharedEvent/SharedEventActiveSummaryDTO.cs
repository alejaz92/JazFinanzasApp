namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventActiveSummaryDTO
    {
        public int EventId { get; set; }
        public string Name { get; set; }
        public List<SharedEventActiveSummaryBalanceDTO> Balances { get; set; } = new();
    }
}
