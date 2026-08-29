using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class TransactionClass : BaseEntity
    {
        [Required]
        public string Description { get; set; }
        public string IncExp {  get; set; }
        public bool IsSystem { get; set; } = false;

        // Jerarquía de un solo nivel (T13, plan-rediseno-reportes.md): una categoría puede colgar
        // de otra, pero un padre no puede a su vez tener padre. Validado en el service, no acá.
        [ForeignKey("ParentId")]
        public int? ParentId { get; set; }
        public TransactionClass? Parent { get; set; }

        // ESSENTIAL / DISCRETIONARY / SAVING (ver TransactionClassNature) — opcional.
        public string? Nature { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
