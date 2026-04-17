using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAssetRepository : IGenericRepository<Asset>
    {
        Task<Asset> GetAssetByIdAsync(int id);
        Task<Asset> GetAssetByNameAsync(string name);
        Task<IEnumerable<Asset>> GetAssetsAsync();
        Task<IEnumerable<Asset>> GetAssetsByTypeAsync(int assetTypeId);
    }
}
