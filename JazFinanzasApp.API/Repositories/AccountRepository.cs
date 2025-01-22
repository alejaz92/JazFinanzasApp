using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        private readonly ApplicationDbContext _context;
        public AccountRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        //get accounts by asset type
        public async Task<IEnumerable<Account>> GetByAssetType(int assetTypeId, int userId) {
            return _context.Accounts
                .Include(a => a.Account_AssetTypes)
                .ThenInclude(a => a.AssetType)
                .Where(a => a.UserId == userId)
                .Where(a => a.Account_AssetTypes.Any(a => a.AssetTypeId == assetTypeId))
                .OrderBy(a => a.Name)
                .ToList();
                
        }

        //check if account used in transactions
        public async Task<bool> IsAccountUsedInTransactions(int accountId) {
            return await _context.Transactions.AnyAsync(t => t.AccountId == accountId);
        }
    }
}
