using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class SharedExpenseRepository : GenericRepository<SharedExpense>, ISharedExpenseRepository
    {
        private readonly ApplicationDbContext _context;

        public SharedExpenseRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SharedExpense?> GetByTransactionIdAsync(int transactionId)
        {
            return await _context.SharedExpenses
                .Include(se => se.Splits)
                    .ThenInclude(s => s.Person)
                .FirstOrDefaultAsync(se => se.TransactionId == transactionId);
        }

        public async Task<IEnumerable<SharedExpenseSplit>> GetPendingSplitsByUserIdAsync(int userId)
        {
            return await _context.SharedExpenseSplits
                .Include(s => s.Person)
                .Include(s => s.SharedExpense)
                .Where(s => s.SharedExpense.UserId == userId && s.Status != SharedExpenseSplitStatus.Paid)
                .ToListAsync();
        }

        public async Task UpdateSplitAsync(SharedExpenseSplit split)
        {
            _context.SharedExpenseSplits.Update(split);
            await _context.SaveChangesAsync();
        }
    }
}
