using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.Person
{
    public class PersonEditDTO
    {
        [Required]
        public string Name { get; set; }
        public string? Alias { get; set; }
    }
}
