using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    [PrimaryKey(nameof(TransactionId), nameof(TagId))]
    public class TransactionTag
    {
        [Required]
        [ForeignKey("TransactionId")]
        public int TransactionId { get; set; }
        public Transaction Transaction { get; set; }

        [Required]
        [ForeignKey("TagId")]
        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
