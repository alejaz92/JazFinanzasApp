using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Models.Domain
{
    public class InvestmentTransaction : BaseEntity
    {
        [Required]
        public DateTime Date { get; set; }
        [Required]  
        public string Environment { get; set; }
        [Required]
        public string MovementType { get; set; }   
        [Required]
        public string CommerceType { get; set; }

        [ForeignKey("TransactionId")]
        public int? ExpenseTransactionId { get; set; }
        public Transaction? ExpenseTransaction { get; set; }
        [ForeignKey("TransactionId")]
        public int? IncomeTransactionId { get; set; }
        public Transaction? IncomeTransaction { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
