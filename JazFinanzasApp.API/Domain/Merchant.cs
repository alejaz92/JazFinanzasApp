using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Comercio deducido del detalle de los movimientos (D-2a / T7, plan-rediseno-reportes.md):
    // no se carga, se agrupa solo a partir de lo que ya se escribió.
    public class Merchant : BaseEntity
    {
        // Nombre canónico mostrado al usuario — el detalle original de la primera transacción
        // que lo creó, o el nombre que el usuario le puso al renombrarlo.
        [Required]
        public string Name { get; set; }

        // true cuando el usuario lo renombró/confirmó explícitamente; false si sigue tal cual
        // lo dejó el resolver automático — distingue "agrupado a ciegas" de "revisado".
        public bool IsConfirmed { get; set; } = false;

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
