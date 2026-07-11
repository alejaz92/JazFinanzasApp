using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedEventPaymentAllocation : BaseEntity
    {
        [Required]
        [ForeignKey("SharedEventPaymentId")]
        public int SharedEventPaymentId { get; set; }
        public SharedEventPayment SharedEventPayment { get; set; }

        // ítem "a mi favor" saldado (motor V1)
        [ForeignKey("SharedExpenseSplitId")]
        public int? SharedExpenseSplitId { get; set; }
        public SharedExpenseSplit? SharedExpenseSplit { get; set; }

        // ítem "deuda mía" saldado
        [ForeignKey("SharedEventMovementShareId")]
        public int? SharedEventMovementShareId { get; set; }
        public SharedEventMovementShare? SharedEventMovementShare { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // egreso categorizado creado (deuda mía)
        [ForeignKey("CreatedExpenseTransactionId")]
        public int? CreatedExpenseTransactionId { get; set; }
        public Transaction? CreatedExpenseTransaction { get; set; }

        // ingreso placeholder creado (pool tarjeta)
        [ForeignKey("CreatedIncomeTransactionId")]
        public int? CreatedIncomeTransactionId { get; set; }
        public Transaction? CreatedIncomeTransaction { get; set; }

        // par EX si hubo cruce de cuentas
        [ForeignKey("CreatedExchangeOutTransactionId")]
        public int? CreatedExchangeOutTransactionId { get; set; }
        public Transaction? CreatedExchangeOutTransaction { get; set; }

        [ForeignKey("CreatedExchangeInTransactionId")]
        public int? CreatedExchangeInTransactionId { get; set; }
        public Transaction? CreatedExchangeInTransaction { get; set; }
    }
}
