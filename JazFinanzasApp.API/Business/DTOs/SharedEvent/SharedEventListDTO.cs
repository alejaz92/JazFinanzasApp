namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsClosed { get; set; }
        public int ParticipantCount { get; set; }
        public int MovementCount { get; set; }
    }
}
