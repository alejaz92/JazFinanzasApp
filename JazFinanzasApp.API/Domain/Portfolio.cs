using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class Portfolio : BaseEntity
    {
        public string Name { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
