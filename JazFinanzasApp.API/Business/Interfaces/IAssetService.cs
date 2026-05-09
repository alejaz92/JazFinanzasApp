using JazFinanzasApp.API.Business.DTO.Asset;
using AssetTypeDTO = JazFinanzasApp.API.Business.DTO.AssetType.AssetTypeDTO;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IAssetService
    {
        Task<IEnumerable<AssetDTO>> GetAllAssetsAsync();
        Task<IEnumerable<AssetTypeDTO>> GetAssetTypesAsync();
        Task<IEnumerable<AssetTypeDTO>> GetAssetTypesByEnvironmentAsync(string environment);
        Task<IEnumerable<AssetDTO>> GetAssetsByTypeAsync(int assetTypeId);
        Task<IEnumerable<AssetDTO>> GetUserAssetsAsync(int userId, int assetTypeId);
        Task<IEnumerable<AssetDTO>> GetUserAssetsByTypeNameAsync(int userId, string assetTypeName);
        Task<IEnumerable<AssetDTO>> GetAssetsForCardTransactionsAsync();
        Task<IEnumerable<AssetDTO>> GetReferenceAssetsAsync(int userId);
        Task AssignAssetToUserAsync(int userId, int assetId);
        Task UnassignAssetFromUserAsync(int userId, int assetId);
        Task UpdateReferenceAsync(int userId, AssetDTO dto);
        Task UpdateMainReferenceAsync(int userId, AssetDTO dto);
    }
}
