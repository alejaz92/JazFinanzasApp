using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.Trip
{
    public class TripAssociationsDTO
    {
        [Required]
        public List<TripMovementRefDTO> Movements { get; set; } = new();
    }
}
