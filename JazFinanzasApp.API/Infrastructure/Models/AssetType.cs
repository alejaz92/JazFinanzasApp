using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Infrastructure.Domain
{
    public class AssetType : BaseEntity
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Environment { get; set; }
        public ICollection<Asset> Assets { get; set; }
    }
}
