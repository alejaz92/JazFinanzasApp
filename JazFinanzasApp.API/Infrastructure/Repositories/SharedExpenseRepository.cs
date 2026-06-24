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

        public async Task<SharedExpense?> GetByCardTransactionIdAsync(int cardTransactionId)
        {
            return await _context.SharedExpenses
                .Include(se => se.Splits)
                    .ThenInclude(s => s.Person)
                .FirstOrDefaultAsync(se => se.CardTransactionId == cardTransactionId);
        }

        public async Task<SharedExpenseSplit?> GetSplitByIdAsync(int splitId)
        {
            return await _context.SharedExpenseSplits
                .Include(s => s.SharedExpense)
                .Include(s => s.Person)
                .FirstOrDefaultAsync(s => s.Id == splitId);
        }

        public async Task AddReimbursementAsync(SharedExpenseReimbursement reimbursement)
        {
            await _context.SharedExpenseReimbursements.AddAsync(reimbursement);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SharedExpenseReimbursement>> GetReimbursementsBySplitIdAsync(int splitId)
        {
            return await _context.SharedExpenseReimbursements
                .Where(r => r.SharedExpenseSplitId == splitId)
                .OrderBy(r => r.Id)
                .ToListAsync();
        }

        public async Task DeleteReimbursementAsync(int id)
        {
            var reimbursement = await _context.SharedExpenseReimbursements.FindAsync(id);
            if (reimbursement != null)
            {
                _context.SharedExpenseReimbursements.Remove(reimbursement);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<SharedExpenseSplit>> GetPendingSplitsByUserIdAsync(int userId)
        {
            return await _context.SharedExpenseSplits
                .Include(s => s.Person)
                .Include(s => s.SharedExpense)
                    .ThenInclude(se => se.Transaction)
                .Include(s => s.SharedExpense)
                    .ThenInclude(se => se.CardTransaction)
                .Where(s => s.SharedExpense.UserId == userId)
                .ToListAsync();
        }

        public async Task UpdateSplitAsync(SharedExpenseSplit split)
        {
            _context.SharedExpenseSplits.Update(split);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByTransactionIdAsync(int transactionId)
        {
            var sharedExpense = await _context.SharedExpenses
                .Include(se => se.Splits)
                .FirstOrDefaultAsync(se => se.TransactionId == transactionId);

            if (sharedExpense == null)
                return;

            _context.SharedExpenseSplits.RemoveRange(sharedExpense.Splits);
            _context.SharedExpenses.Remove(sharedExpense);
            await _context.SaveChangesAsync();
        }
    }
}
