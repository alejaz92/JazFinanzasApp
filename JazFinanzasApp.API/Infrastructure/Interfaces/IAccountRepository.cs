using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<IEnumerable<Account>> GetByAssetType(int assetTypeId, int userId);
        Task<bool> IsAccountUsedInTransactions(int accountId);
        Task<IEnumerable<Account>> GetByUserIdAsync(int userId);
    }
}
