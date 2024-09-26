using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Asset;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAssetRepository : IGenericRepository<Asset>
    {
        Task<IEnumerable<AssetDTO>> GetAllAssetsAsync();
    }
}
