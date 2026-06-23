using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedExpenseReimbursement : BaseEntity
    {
        [Required]
        [ForeignKey("SharedExpenseSplitId")]
        public int SharedExpenseSplitId { get; set; }
        public SharedExpenseSplit SharedExpenseSplit { get; set; }

        [Required]
        [ForeignKey("TransactionId")]
        public int TransactionId { get; set; }
        public Transaction Transaction { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public int? InstallmentNumber { get; set; }
    }
}
