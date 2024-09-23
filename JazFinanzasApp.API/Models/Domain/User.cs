using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.Domain
{
    public class User : BaseEntity
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Email { get; set; }
        //public byte[] PasswordHash { get; set; }
        //public byte[] PasswordSalt { get; set; }

        public bool IsActive { get; set; }  = true;

    }
}
