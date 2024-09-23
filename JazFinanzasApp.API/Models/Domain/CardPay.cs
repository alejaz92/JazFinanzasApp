using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.Domain
{
    public class CardPay
    {
        [Key]
        [Required]
        [ForeignKey("CardId")]
        public int CardId { get; set; }
        public Card Card { get; set; }

        [Key]
        [Required]
        [ForeignKey("DateId")]
        public int DateId { get; set; }
        public Date Date { get; set; }
    }
}
