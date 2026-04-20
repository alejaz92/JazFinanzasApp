using JazFinanzasApp.API.Business.DTO.Account;
using JazFinanzasApp.API.Business.DTO.Account_AssetType;
using JazFinanzasApp.API.Business.DTO.AssetType;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IAccountService
    {
        Task<IEnumerable<AccountDTO>> GetAllForUserAsync(int userId);
        Task<AccountDTO> GetByIdAsync(int userId, int id);
        Task<IEnumerable<AccountDTO>> GetByAssetTypeAsync(int userId, int assetTypeId);
        Task<IEnumerable<AccountDTO>> GetByAssetTypeNameAsync(int userId, string assetTypeName);
        Task CreateAccountAsync(int userId, AccountDTO dto);
        Task UpdateAccountAsync(int userId, int id, AccountDTO dto);
        Task DeleteAccountAsync(int userId, int id);
        Task<IEnumerable<Account_AssetTypeDTO>> GetAssetTypesForAccountAsync(int userId, int accountId);
        Task AssignAssetTypesToAccountAsync(int userId, int accountId, List<Account_AssetTypeDTO> assetTypes);
    }
}
