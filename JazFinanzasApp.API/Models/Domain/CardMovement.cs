using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.Domain
{
    public class CardMovement : BaseEntity
    {
        [Required]
        public DateTime DateMovement {  get; set; }

        public string Detail {  get; set; }

        [Required]
        [ForeignKey("CardId")]
        public int CardId { get; set; }
        public Card Card { get; set; }

        [Required]
        [ForeignKey("MovementClassId")]
        public int MovementClassId { get; set; }
        public MovementClass MovementClass { get; set; }

        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public int Installments { get; set; }

        [Required]
        public DateTime FirstInstallment {  get; set; }

        public DateTime LastInstallment { get; set; }

        [Required]
        public string Repeat {  get; set; }

        public decimal InstallmentAmount { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
