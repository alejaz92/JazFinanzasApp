using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Infrastructure.Domain
{
    public class User : IdentityUser<int>
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }

        public bool IsActive { get; set; }  = true;

        [Required]
        public DateTime CreatedAt { get; set; }  = DateTime.UtcNow;
        [Required]
        public DateTime UpdatedAt { get; set; }  = DateTime.UtcNow;

    }
}
