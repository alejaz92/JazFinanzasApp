﻿using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAsset_UserRepository : IGenericRepository<Asset_User>
    {
        Task AssignAssetToUserAsync(int userId, int assetId);
        Task<IEnumerable<Asset_User>> GetReferenceAssetsAsync(int userId);
        Task<Asset_User> GetUserAssetAsync(int userId, int assetId);
        Task<IEnumerable<Asset_User>> GetUserAssetsAsync(int userId, int assetTypeId);
        Task<bool> IsAssetUserInUseAsync(int userId, int assetId);
        Task UnassignAssetToUserAsync(int userId, int assetId);
    }
}
