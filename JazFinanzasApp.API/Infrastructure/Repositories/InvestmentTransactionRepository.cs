using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class InvestmentTransactionRepository : GenericRepository<InvestmentTransaction>, IInvestmentTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public InvestmentTransactionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }


        public async Task<(IEnumerable<InvestmentTransaction> Transactions, int TotalCount)> GetPaginatedInvestmentTransactions(int userId, int page, int pageSize, string environment)
        {
            var totalCount = await _context.InvestmentTransactions
                .Where(m => m.UserId == userId)
                .Where(m => m.Environment == environment)
                .CountAsync();

            var transactions = await _context.InvestmentTransactions
                .Where(m => m.UserId == userId)
                .Where(m => m.Environment == environment)
                .Include(m => m.IncomeTransaction)
                .Include(m => m.IncomeTransaction.Asset)
                .Include(m => m.IncomeTransaction.Asset.AssetType)
                .Include(m => m.IncomeTransaction.Account)
                .Include(m => m.IncomeTransaction.Portfolio)
                .Include(m => m.ExpenseTransaction)
                .Include(m => m.ExpenseTransaction.Asset)
                .Include(m => m.ExpenseTransaction.Asset.AssetType)
                .Include(m => m.ExpenseTransaction.Account)
                .Include(m => m.ExpenseTransaction.Portfolio)
                .OrderByDescending(m => m.Date)
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (transactions, totalCount);
        }

        public async Task<InvestmentTransaction> GetInvestmentTransactionById(int id)
        {
            return await _context.InvestmentTransactions
                .Include(m => m.IncomeTransaction)
                .Include(m => m.IncomeTransaction.Asset)
                .Include(m => m.IncomeTransaction.Account)
                .Include(m => m.IncomeTransaction.Portfolio)
                .Include(m => m.ExpenseTransaction)
                .Include(m => m.ExpenseTransaction.Asset)
                .Include(m => m.ExpenseTransaction.Account)
                .Include(m => m.ExpenseTransaction.Portfolio)
                .FirstOrDefaultAsync(m => m.Id == id);
        }
               
    }

}
