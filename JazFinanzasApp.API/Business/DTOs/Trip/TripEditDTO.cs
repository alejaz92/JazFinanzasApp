using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.Trip
{
    public class TripEditDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}
