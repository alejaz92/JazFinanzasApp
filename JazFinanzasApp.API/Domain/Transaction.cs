using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    
    public class Transaction : BaseEntity
    {

        [Required]
        [ForeignKey("AccountId")]
        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        [ForeignKey("PortfolioId")]
        public int PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; }

        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string MovementType { get; set; }

        
        [ForeignKey("TransactionClassId")]
        public int? TransactionClassId { get; set; }
        public TransactionClass? TransactionClass { get; set; }

        // Vínculo al consumo de tarjeta de origen cuando la transacción es el pago de una cuota
        [ForeignKey("CardTransactionId")]
        public int? CardTransactionId { get; set; }
        public CardTransaction? CardTransaction { get; set; }

        [ForeignKey("TripId")]
        public int? TripId { get; set; }
        public Trip? Trip { get; set; }

        public string? Detail {  get; set; }

        [Required]
        [Column(TypeName = "decimal(18,10)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        public decimal? QuotePrice { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
