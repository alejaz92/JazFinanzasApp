using JazFinanzasApp.API.Business.DTO.Account_AssetType;
using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAccount_AssetTypeRepository : IGenericRepository<Account_AssetType>
    {
        Task<bool> AssignAssetTypesToAccountAsync(int accountId, List<Account_AssetTypeDTO> assetTypes);
        Task<Account_AssetType> GetAccount_AssetTypeByAccountIdAndAssetTypeNameAsync(int accountId, string assetTypeName);
        Task<IEnumerable<Account_AssetTypeDTO>> GetAssetTypes(int id);
    }
}
