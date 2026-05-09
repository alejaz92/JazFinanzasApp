using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAccount_AssetTypeRepository : IGenericRepository<Account_AssetType>
    {
        Task<bool> AssignAssetTypesToAccountAsync(int accountId, IEnumerable<AccountAssetTypeResult> assetTypes);
        Task<Account_AssetType> GetAccount_AssetTypeByAccountIdAndAssetTypeNameAsync(int accountId, string assetTypeName);
        Task<IEnumerable<AccountAssetTypeResult>> GetAssetTypes(int id);
    }
}
