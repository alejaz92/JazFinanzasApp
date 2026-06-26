using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class CardTransactionDiscount : BaseEntity
    {
        [Required]
        [ForeignKey("CardTransactionId")]
        public int CardTransactionId { get; set; }
        public CardTransaction CardTransaction { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountApplied { get; set; } = 0;

        public string? Notes { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
