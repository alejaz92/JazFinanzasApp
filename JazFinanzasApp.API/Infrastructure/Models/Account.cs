using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Infrastructure.Domain
{
    public class Account : BaseEntity
    {
        public string Name { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<Account_AssetType> Account_AssetTypes { get; set; }

    }
}
