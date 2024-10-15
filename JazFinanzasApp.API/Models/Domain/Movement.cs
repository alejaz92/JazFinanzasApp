using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Models.Domain
{
    [PrimaryKey(nameof(Id), nameof(AccountId), nameof(AssetId))]
    public class Movement
    {
        [Required]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ForeignKey("AccountId")]
        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string MovementType { get; set; }

        
        [ForeignKey("MovementClassId")]
        public int? MovementClassId { get; set; }
        public MovementClass? MovementClass { get; set; }

        public string? Detail {  get; set; }

        [Required]
        [Column(TypeName = "decimal(18,10)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        public decimal? QuotePrice { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
