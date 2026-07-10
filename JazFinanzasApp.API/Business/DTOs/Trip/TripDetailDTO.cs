namespace JazFinanzasApp.API.Business.DTO.Trip
{
    public class TripDetailDTO : TripDTO
    {
        public List<TripMovementDTO> Movements { get; set; } = new();
    }
}
