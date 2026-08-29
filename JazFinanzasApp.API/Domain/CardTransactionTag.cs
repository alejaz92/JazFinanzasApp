using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Domain
{
    [PrimaryKey(nameof(CardTransactionId), nameof(TagId))]
    public class CardTransactionTag
    {
        [Required]
        [ForeignKey("CardTransactionId")]
        public int CardTransactionId { get; set; }
        public CardTransaction CardTransaction { get; set; }

        [Required]
        [ForeignKey("TagId")]
        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
