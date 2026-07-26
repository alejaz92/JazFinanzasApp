namespace JazFinanzasApp.API.Business.DTO.Trip
{
    public class TripDetailDTO : TripDTO
    {
        public List<TripMovementDTO> Movements { get; set; } = new();
        public List<TripLinkedEventDTO> LinkedEvents { get; set; } = new();
    }
}
