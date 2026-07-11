namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Notes { get; set; }
        public bool IsClosed { get; set; }
        public List<SharedEventParticipantDTO> Participants { get; set; } = new();
        public List<SharedEventMovementDTO> Movements { get; set; } = new();
        public List<SharedEventBalanceDTO> Balances { get; set; } = new();
        public List<SharedEventCategoryTotalDTO> CategoryTotals { get; set; } = new();
    }
}
