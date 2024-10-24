﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Models.Domain
{
    public class Asset : BaseEntity
    {
        [Required]
        public string Name {  get; set; }
        [Required]
        public string Symbol {  get; set; }
        [Required]
        public int AssetTypeId { get; set; }
        [Required]
        public AssetType AssetType { get; set; }



    }
}
