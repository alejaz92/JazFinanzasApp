using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Domain
{
    [PrimaryKey(nameof(CardId), nameof(Date))]
    public class CardPayment
    {
        [Required]
        [ForeignKey("CardId")]
        public int CardId { get; set; }
        public Card Card { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }
}
