﻿using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.DTO.User
{
    public class EditUserDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

    }
}
