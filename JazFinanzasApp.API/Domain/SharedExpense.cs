using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedExpense : BaseEntity
    {
        [ForeignKey("TransactionId")]
        public int? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        [ForeignKey("CardTransactionId")]
        public int? CardTransactionId { get; set; }
        public CardTransaction? CardTransaction { get; set; }

        public string? Notes { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<SharedExpenseSplit> Splits { get; set; }
    }
}
