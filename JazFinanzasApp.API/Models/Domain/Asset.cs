using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Models.Domain
{
    public class Asset : BaseEntity
    {
        [Required]
        public string Name {  get; set; }
        [Required]
        public string Symbol {  get; set; }
        [Required]
        [ForeignKey("AssetTypeId")]
        public int AssetTypeId { get; set; }
        public AssetType AssetType { get; set; }



    }
}
