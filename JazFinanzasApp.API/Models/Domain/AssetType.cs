using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.Domain
{
    public class AssetType : BaseEntity
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Environment { get; set; }
    }
}
