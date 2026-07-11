using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventAddDTO
    {
        [Required]
        public string Name { get; set; }
        public string? Notes { get; set; }
        public List<int> PersonIds { get; set; } = new();
    }
}
