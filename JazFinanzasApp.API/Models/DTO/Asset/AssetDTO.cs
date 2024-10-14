using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Models.DTO.Asset
{
    public class AssetDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string AssetTypeName { get; set; }
    }
}
