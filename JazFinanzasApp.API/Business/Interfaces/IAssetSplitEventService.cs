using JazFinanzasApp.API.Business.DTO.AssetSplitEvent;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IAssetSplitEventService
    {
        Task<IEnumerable<AssetSplitEventListDTO>> GetByAssetIdAsync(int assetId);
        Task AddAsync(AssetSplitEventAddDTO dto, int userId);
        Task DeleteAsync(int id, int userId);
    }
}
