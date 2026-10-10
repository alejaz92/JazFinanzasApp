using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Qué pasó con un pago del cronograma (BondPayment) para una tenencia puntual: en qué cuenta y
    // cartera estaba el bono, cuánto se cobró (separado en capital e interés) y a qué fecha real.
    // Índice único (BondPaymentId, UserId, AccountId, PortfolioId): un solo registro por pago y
    // tenencia (ver plan-amortizaciones-bonos.md, Etapa 2 — Modelo de datos).
    public class BondCollection : BaseEntity
    {
        [Required]
        [ForeignKey("BondPaymentId")]
        public int BondPaymentId { get; set; }
        public BondPayment BondPayment { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [ForeignKey("AccountId")]
        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        [ForeignKey("PortfolioId")]
        public int PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; }

        // Registered / Untracked / Dismissed, ver BondCollectionStatus.
        [Required]
        [MaxLength(10)]
        public string Status { get; set; }

        // Tenencia en esta cuenta y cartera el día anterior al pago — queda guardada para auditar la
        // estimación que se le propuso al usuario al registrar.
        [Required]
        [Column(TypeName = "decimal(18,10)")]
        public decimal HeldQuantity { get; set; }

        // Fecha real de acreditación (suele ser unos días posterior a BondPayment.PaymentDate).
        // Irrelevante si Status es Dismissed.
        public DateTime? CollectionDate { get; set; }

        // En la moneda de BondPayment.CurrencyAssetId. En 0 si Status es Dismissed.
        [Required]
        [Column(TypeName = "decimal(18,10)")]
        public decimal CapitalAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,10)")]
        public decimal InterestAmount { get; set; } = 0;

        // Cotización de la moneda del pago a CollectionDate, resuelta igual que la de un movimiento
        // (QuotePriceResolver) — permite valuar en la moneda de referencia sin otra consulta.
        [Column(TypeName = "decimal(18,10)")]
        public decimal? QuotePrice { get; set; }
    }
}
