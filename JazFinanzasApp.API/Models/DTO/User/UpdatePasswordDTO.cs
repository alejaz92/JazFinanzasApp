﻿namespace JazFinanzasApp.API.Models.DTO.User
{
    public class UpdatePasswordDTO
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}