using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.Tag
{
    public class TagAddDTO
    {
        [Required]
        public string Name { get; set; }
        public string? Color { get; set; }
    }
}
