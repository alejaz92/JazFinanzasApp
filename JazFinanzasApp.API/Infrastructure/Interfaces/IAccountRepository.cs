using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<IEnumerable<Account>> GetByAssetType(int assetTypeId, int userId);
        Task<bool> IsAccountUsedInTransactions(int accountId);
    }
}
