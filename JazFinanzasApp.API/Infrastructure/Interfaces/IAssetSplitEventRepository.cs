using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAssetSplitEventRepository
    {
        Task<IEnumerable<AssetSplitEvent>> GetByAssetIdAsync(int assetId);
        Task<AssetSplitEvent> GetByIdAsync(int id);
        Task AddAsync(AssetSplitEvent splitEvent);
        Task DeleteAsync(int id);
    }
}
