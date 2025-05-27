using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Infrastructure.Domain
{
    public class TransactionClass : BaseEntity
    {
        [Required]
        public string Description { get; set; }
        public string IncExp {  get; set; }
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
