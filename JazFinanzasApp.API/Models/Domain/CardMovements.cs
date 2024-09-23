using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.Domain
{
    public class CardMovements : BaseEntity
    {
        [Required]
        [ForeignKey("DateMovement")]
        public int DateMovementId { get; set; }
        public Date DateMovement {  get; set; }

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
        [ForeignKey("FirstInstallmentId")]
        public int FirstInstallmentId { get; set; }
        public Date FirstInstallment {  get; set; }


        [Required]
        [ForeignKey("LastInstallmentId")]
        public int LastInstallmentId { get; set; }
        public Date LastInstallment { get; set; }

        [Required]
        public string Repeat {  get; set; }

        public decimal InstallmentAmount { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
