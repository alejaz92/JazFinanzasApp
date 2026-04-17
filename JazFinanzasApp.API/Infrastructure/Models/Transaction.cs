using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Infrastructure.Domain
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
<<<<<<< HEAD:JazFinanzasApp.API/Models/Domain/Transaction.cs
        public Portfolio? Portfolio { get; set; }
=======
        public Portfolio Portfolio { get; set; }
>>>>>>> 394b53c6920c20dcc673d0463ef6ee61cdce2eeb:JazFinanzasApp.API/Infrastructure/Models/Transaction.cs

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
