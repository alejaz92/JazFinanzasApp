using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class ReportFavorite : BaseEntity
    {
        // Identifica la vista de reporte favoriteada (ruta + query params si el reporte los usa,
        // ej. "shared-events-by-person?personId=3") — mismo criterio que "enlaces que se pueden
        // guardar" (sección 7 del plan). Se interpreta del lado del frontend, el backend solo lo guarda.
        [Required]
        public string ReportKey { get; set; }

        // Orden manual para la fila de favoritos en Inicio (5.7) — se asigna al final al crear y se
        // puede reordenar.
        public int Order { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
