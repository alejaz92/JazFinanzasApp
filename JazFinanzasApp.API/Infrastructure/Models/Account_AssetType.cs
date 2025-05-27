using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Infrastructure.Domain
{
    [PrimaryKey(nameof(AccountId), nameof(AssetTypeId))]
    public class Account_AssetType
    {
        [Required]
        [ForeignKey("AccountId")]
        public int AccountId { get; set; }
        public Account Account { get; set; }   

        [Required]
        [ForeignKey("AssetTypeId")]
        public int AssetTypeId { get; set; }
        public AssetType AssetType { get; set; }
    }
}
