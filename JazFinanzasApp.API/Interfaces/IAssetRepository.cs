using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Asset;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAssetRepository : IGenericRepository<Asset>
    {
        Task<Asset> GetAssetByIdAsync(int id);
        Task<Asset> GetAssetByNameAsync(string name);
        Task<IEnumerable<Asset>> GetAssetsAsync();
        Task<IEnumerable<Asset>> GetAssetsByTypeAsync(int assetTypeId);
    }
}
