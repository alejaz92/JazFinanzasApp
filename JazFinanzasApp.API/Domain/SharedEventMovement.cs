using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedEventMovement : BaseEntity
    {
        [Required]
        [ForeignKey("SharedEventId")]
        public int SharedEventId { get; set; }
        public SharedEvent SharedEvent { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [ForeignKey("TransactionClassId")]
        public int TransactionClassId { get; set; }
        public TransactionClass TransactionClass { get; set; }

        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // null = pagó el usuario
        [ForeignKey("PayerPersonId")]
        public int? PayerPersonId { get; set; }
        public Person? PayerPerson { get; set; }

        // egreso real creado si el usuario pagó por cuenta
        [ForeignKey("TransactionId")]
        public int? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        // consumo real creado si el usuario pagó con tarjeta
        [ForeignKey("CardTransactionId")]
        public int? CardTransactionId { get; set; }
        public CardTransaction? CardTransaction { get; set; }

        // motor V1 creado si el usuario pagó y hay shares de terceros
        [ForeignKey("SharedExpenseId")]
        public int? SharedExpenseId { get; set; }
        public SharedExpense? SharedExpense { get; set; }

        public string? Notes { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<SharedEventMovementShare> Shares { get; set; }
    }
}
