using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.Tag
{
    public class TagEditDTO
    {
        [Required]
        public string Name { get; set; }
        public string? Color { get; set; }
    }
}
