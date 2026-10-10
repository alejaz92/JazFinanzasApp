using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Cronograma de pagos de un bono u obligación negociable: una fila por fecha de pago, fija desde
    // que el activo existe en el catálogo (ver plan-amortizaciones-bonos.md, DA-1) — se carga por
    // script SQL (docs/scripts/cronogramas-bonos.sql), nunca por pantalla ni por migración.
    public class BondPayment : BaseEntity
    {
        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        // Interés por cada 100 nominales ORIGINALES del bono (no sobre el capital residual). En un
        // bono indexado (IsIndexed) es solo informativo: el monto real se carga al registrar el cobro.
        [Required]
        [Column(TypeName = "decimal(18,10)")]
        public decimal InterestPer100 { get; set; }

        // Fracción (no porcentaje: 0.08 = 8%) del capital original que amortiza en esta fecha.
        [Required]
        [Column(TypeName = "decimal(18,10)")]
        public decimal AmortizationRate { get; set; }

        // Moneda en la que paga el bono (ARS/USD, según el activo).
        [Required]
        [ForeignKey("CurrencyAssetId")]
        public int CurrencyAssetId { get; set; }
        public Asset CurrencyAsset { get; set; }

        // Bonos CER / dólar linked: el monto por cada 100 nominales depende del ajuste y no se conoce
        // de antemano. InterestPer100 queda sin uso para estimar (el formulario no propone importe).
        public bool IsIndexed { get; set; } = false;
    }
}
