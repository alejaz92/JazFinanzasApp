using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.Trip
{
    // Referencia tipo+id a un movimiento (transacción de cuenta o consumo de tarjeta)
    public class TripMovementRefDTO
    {
        [Required]
        public string Type { get; set; } // ACCOUNT / CARD

        [Required]
        public int Id { get; set; }
    }
}
