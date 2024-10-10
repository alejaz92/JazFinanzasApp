using Microsoft.Identity.Client;

namespace JazFinanzasApp.API.Models.DTO.movementClasses
{
    public class MovementClassDTO
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string IncExp { get; set; }
    }
}
