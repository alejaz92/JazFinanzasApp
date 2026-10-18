using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Habilitación del asistente (devoluciones con LLM) para un usuario y su tope de gasto mensual
    // en dólares. Sin fila equivale a deshabilitado (plan-devoluciones-llm.md, D12).
    public class AssistantUserSettings : BaseEntity
    {
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public bool Enabled { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyBudgetUsd { get; set; }
    }
}
