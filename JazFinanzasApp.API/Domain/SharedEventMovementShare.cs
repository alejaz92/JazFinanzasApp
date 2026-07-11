using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedEventMovementShare : BaseEntity
    {
        [Required]
        [ForeignKey("SharedEventMovementId")]
        public int SharedEventMovementId { get; set; }
        public SharedEventMovement SharedEventMovement { get; set; }

        // null = la parte del usuario
        [ForeignKey("PersonId")]
        public int? PersonId { get; set; }
        public Person? Person { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountSettled { get; set; } = 0;

        // si el movimiento lo pagó el usuario: split del motor V1 de esta persona
        [ForeignKey("SharedExpenseSplitId")]
        public int? SharedExpenseSplitId { get; set; }
        public SharedExpenseSplit? SharedExpenseSplit { get; set; }
    }
}
