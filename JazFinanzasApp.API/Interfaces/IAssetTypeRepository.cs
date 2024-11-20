using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAssetTypeRepository : IGenericRepository<AssetType>
    {
        Task<IEnumerable<AssetType>> GetAssetTypes(string environment);
        Task<AssetType> GetByName(string name);
        Task<int> GetIdByName(string name);
    }
}
