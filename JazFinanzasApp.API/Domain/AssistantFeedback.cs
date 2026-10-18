using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Calificación del usuario sobre una devolución (plan-devoluciones-llm.md, Flujo 4). Una por
    // devolución: calificar de nuevo reemplaza la anterior.
    public class AssistantFeedback : BaseEntity
    {
        [Required]
        [ForeignKey("ReviewId")]
        public int ReviewId { get; set; }
        public AssistantReview Review { get; set; }

        // +1 útil / -1 no útil.
        public int Rating { get; set; }

        // "Tiene un dato mal": el usuario encontró una cifra incorrecta.
        public bool HasWrongFigures { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
