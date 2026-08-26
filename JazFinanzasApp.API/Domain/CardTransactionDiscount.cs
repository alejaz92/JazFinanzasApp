using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class CardTransactionDiscount : BaseEntity
    {
        [Required]
        [ForeignKey("CardTransactionId")]
        public int CardTransactionId { get; set; }
        public CardTransaction CardTransaction { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Cuánto del descuento ya se consumió dentro de una cuota de la tarjeta.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountApplied { get; set; } = 0;

        // Cuánto del descuento ya salió de la tarjeta y vive como ingreso en alguna cuenta.
        // Pendiente en la tarjeta = Amount - AmountMaterialized.
        // Vivo como ingreso sin consumir = AmountMaterialized - AmountApplied.
        // Invariante: 0 <= AmountApplied <= AmountMaterialized <= Amount.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountMaterialized { get; set; } = 0;

        // ACCOUNT o CARD, ver CardTransactionDiscountCreditTarget.
        [Required]
        [MaxLength(10)]
        public string CreditTarget { get; set; } = CardTransactionDiscountCreditTarget.Account;

        // Fecha en que el banco acreditó el reintegro. Ordena el consumo cuando una tarjeta
        // tiene varios descuentos con saldo a favor pendiente (se consume primero el más viejo).
        [Required]
        public DateTime CreditDate { get; set; }

        public string? Notes { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
