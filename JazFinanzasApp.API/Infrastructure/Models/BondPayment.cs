using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Infrastructure.Domain
{
    public class BondPayment: BaseEntity
    {
        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }
        [Required]
        public DateTime PaymentDate { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public decimal Income { get; set; }
        [Required]
        public decimal AmortizationPercentage { get; set; }
    }
}
