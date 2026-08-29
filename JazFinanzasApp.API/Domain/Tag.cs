using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Etiqueta libre (sección 7, plan-rediseno-reportes.md): cortes transversales que no
    // encajan en una categoría, ej. "auto", "mascota", "regalos". Opcional por diseño.
    public class Tag : BaseEntity
    {
        [Required]
        public string Name { get; set; }
        public string? Color { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
