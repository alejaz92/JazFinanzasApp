using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedExpenseSplit : BaseEntity
    {
        [Required]
        [ForeignKey("SharedExpenseId")]
        public int SharedExpenseId { get; set; }
        public SharedExpense SharedExpense { get; set; }

        [Required]
        [ForeignKey("PersonId")]
        public int PersonId { get; set; }
        public Person Person { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountReimbursed { get; set; } = 0;

        [Required]
        public SharedExpenseSplitStatus Status { get; set; } = SharedExpenseSplitStatus.Pending;

        public string? Notes { get; set; }
    }
}
