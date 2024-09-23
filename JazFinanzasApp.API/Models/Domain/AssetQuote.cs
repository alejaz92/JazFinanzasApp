using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.Domain
{
    public class AssetQuote
    {
        [Key]
        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Key]
        [Required]
        [ForeignKey("DateId")]
        public int DateId { get; set; }
        public Date Date { get; set; }

        [Key]
        [Required]
        public string Type { get; set; }
        [Required]
        public decimal Value { get; set; }

    }
}
