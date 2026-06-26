using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class CardTransactionDiscountInstallment : BaseEntity
    {
        [Required]
        [ForeignKey("CardTransactionDiscountId")]
        public int CardTransactionDiscountId { get; set; }
        public CardTransactionDiscount CardTransactionDiscount { get; set; }

        [Required]
        [ForeignKey("TransactionId")]
        public int TransactionId { get; set; }
        public Transaction Transaction { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public int InstallmentNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }
}
