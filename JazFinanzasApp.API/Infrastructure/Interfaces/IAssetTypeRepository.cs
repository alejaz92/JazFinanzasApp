using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAssetTypeRepository : IGenericRepository<AssetType>
    {
        Task<IEnumerable<AssetType>> GetAssetTypes(string environment);
        Task<AssetType> GetByName(string name);
        Task<int> GetIdByName(string name);
    }
}
