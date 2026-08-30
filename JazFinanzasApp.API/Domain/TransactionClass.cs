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

        // Si es false, la categoría no cuenta como ingreso ni egreso en los reportes
        // (ajustes de saldo, movimientos de inversión) — reemplaza la comparación
        // por nombre que usaban los reportes actuales.
        public bool CountsAsIncomeExpense { get; set; } = true;

        // Rubro del que cuelga esta categoría. Máximo dos niveles: se valida en el
        // servicio que el padre elegido no tenga a su vez un padre.
        [ForeignKey("ParentId")]
        public int? ParentId { get; set; }
        public TransactionClass? Parent { get; set; }
        public ICollection<TransactionClass>? Children { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
