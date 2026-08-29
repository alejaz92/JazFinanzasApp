using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.Merchant
{
    public class MerchantRenameDTO
    {
        [Required]
        public string Name { get; set; }
    }
}
