using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Domain
{
    public class Account : BaseEntity
    {
        public string Name { get; set; }

        // CASH / BANK / WALLET / INVESTMENT / OTHER (ver AccountType) — opcional.
        public string? Type { get; set; }

        // Separa "disponible" de "invertido" en el reporte de Patrimonio (sección 7,
        // plan-rediseno-reportes.md). Default true: una cuenta nueva cuenta como líquida
        // hasta que se diga lo contrario.
        [Required]
        public bool CountsAsLiquid { get; set; } = true;

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<Account_AssetType> Account_AssetTypes { get; set; }

    }
}
