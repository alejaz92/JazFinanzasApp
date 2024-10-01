﻿using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.DTO.User
{
    public class LoginUserDTO
    {
        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}