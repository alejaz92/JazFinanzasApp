using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAsset_UserRepository : IGenericRepository<Asset_User>
    {
        Task AssignAssetToUserAsync(int userId, int assetId);
        Task<IEnumerable<Asset_User>> GetUserAssetAsync(int userId);
        Task UnassignAssetToUserAsync(int userId, int assetId);
    }
}
