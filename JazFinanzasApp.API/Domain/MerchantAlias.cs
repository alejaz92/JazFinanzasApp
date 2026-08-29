using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Texto normalizado → comercio (T7, plan-rediseno-reportes.md). Como mucho un alias por
    // (usuario, texto normalizado) — la unicidad la garantiza el resolver, no una constraint de
    // base (MerchantAlias no tiene UserId propio; se llega al usuario vía Merchant.UserId).
    public class MerchantAlias : BaseEntity
    {
        [Required]
        [ForeignKey("MerchantId")]
        public int MerchantId { get; set; }
        public Merchant Merchant { get; set; }

        [Required]
        public string NormalizedDetail { get; set; }

        // true = corrección del usuario (renombre, reasignación manual, fusión). Un alias manual
        // nunca se pisa en una re-ejecución del resolver — es la garantía que hace que el
        // mecanismo mejore con el uso en vez de deshacer el trabajo del usuario.
        [Required]
        public bool IsManual { get; set; } = false;
    }
}
