using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Models.Domain
{
    [PrimaryKey(nameof(AssetId), nameof(Date), nameof(Type))]
    public class AssetQuote
    {
        [Key]
        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Type { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,10)")]
        public decimal Value { get; set; }

    }
}
