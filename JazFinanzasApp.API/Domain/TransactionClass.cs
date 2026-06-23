using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class TransactionClass : BaseEntity
    {
        [Required]
        public string Description { get; set; }
        public string IncExp {  get; set; }
        public bool IsSystem { get; set; } = false;
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
