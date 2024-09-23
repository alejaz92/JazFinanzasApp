using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Models.Domain
{
    public class Movement
    {
        [Key]
        [Required]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Key]
        [Required]
        [ForeignKey("AccountId")]
        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Key]
        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Required]
        [ForeignKey("DateId")]
        public int DateId { get; set; }
        public Date Date { get; set; }

        [Required]
        public string MovementType { get; set; }

        [Required]
        [ForeignKey("MovementClassId")]
        public int MovementClassId { get; set; }
        public MovementClass MovementClass { get; set; }

        public string? Detail {  get; set; }

        [Required]
        public decimal Amount { get; set; }

        public decimal? QuotePrice { get; set; }
    }
}
