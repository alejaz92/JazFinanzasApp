namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Notes { get; set; }
        public bool IsClosed { get; set; }
        public List<SharedEventParticipantDTO> Participants { get; set; } = new();
    }
}
