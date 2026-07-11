using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventEditDTO
    {
        [Required]
        public string Name { get; set; }
        public string? Notes { get; set; }
    }
}
