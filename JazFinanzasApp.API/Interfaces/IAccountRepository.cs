using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<IEnumerable<Account>> GetByAssetType(int assetTypeId, int userId);
        Task<bool> IsAccountUsedInTransactions(int accountId);
    }
}
