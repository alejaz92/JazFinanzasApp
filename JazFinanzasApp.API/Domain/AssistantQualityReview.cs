using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Registro de una revisión de calidad del asistente ya hecha (plan-devoluciones-llm.md, Flujo 4).
    // La revisión se hace en una sesión de trabajo, no la hace la app: esta fila la inserta quien la
    // termina. Sirve de historial y para calcular cuándo toca la siguiente (D9).
    public class AssistantQualityReview : BaseEntity
    {
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public DateTime ReviewedAt { get; set; }

        [Required]
        [MaxLength(40)]
        public string PromptVersionBefore { get; set; }

        // Igual a PromptVersionBefore si la revisión no cambió nada.
        [Required]
        [MaxLength(40)]
        public string PromptVersionAfter { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
