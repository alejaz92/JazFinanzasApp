﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Infrastructure.Domain
{
    [PrimaryKey(nameof(UserId), nameof(AssetId))]
    public class Asset_User
    {
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        public bool isReference { get; set; }

        public bool isMainReference { get; set; }
    }
}
