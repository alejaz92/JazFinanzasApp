using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Models.Domain
{
    public class InvestmentMovement : BaseEntity
    {
        [Required]
        public DateTime Date { get; set; }
        [Required]  
        public string Environment { get; set; }
        [Required]
        public string MovementType { get; set; }   
        [Required]
        public string CommerceType { get; set; }

        [ForeignKey("MovementId")]
        public int? ExpenseMovementId { get; set; }
        public Movement? ExpenseMovement { get; set; }
        [ForeignKey("MovementId")]
        public int? IncomeMovementId { get; set; }
        public Movement? IncomeMovement { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
