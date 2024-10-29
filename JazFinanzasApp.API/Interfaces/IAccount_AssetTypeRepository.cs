using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Account_AssetType;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAccount_AssetTypeRepository : IGenericRepository<Account_AssetType>
    {
        Task<bool> AssignAssetTypesToAccountAsync(int accountId, List<Account_AssetTypeDTO> assetTypes);
        Task<Account_AssetType> GetAccount_AssetTypeByAccountIdAndAssetTypeNameAsync(int accountId, string assetTypeName);
        Task<IEnumerable<Account_AssetTypeDTO>> GetAssetTypes(int id);
    }
}
