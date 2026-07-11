using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class SharedEventMovementRepository : GenericRepository<SharedEventMovement>, ISharedEventMovementRepository
    {
        private readonly ApplicationDbContext _context;

        public SharedEventMovementRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SharedEventMovement?> GetDetailByIdAsync(int id)
        {
            return await _context.SharedEventMovements
                .Include(m => m.SharedEvent)
                .Include(m => m.Shares)
                    .ThenInclude(s => s.Person)
                .Include(m => m.SharedExpense)
                    .ThenInclude(se => se.Splits)
                .Include(m => m.TransactionClass)
                .Include(m => m.Asset)
                .Include(m => m.PayerPerson)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<bool> HasActivityAsync(int movementId)
        {
            var movement = await GetDetailByIdAsync(movementId);
            if (movement == null) return false;

            if (movement.Shares.Any(s => s.AmountSettled != 0))
                return true;

            if (movement.SharedExpense != null && movement.SharedExpense.Splits.Any(s => s.AmountReimbursed != 0 || s.AmountApplied != 0))
                return true;

            var shareIds = movement.Shares.Select(s => s.Id).ToList();
            var splitIds = movement.SharedExpense?.Splits.Select(s => s.Id).ToList() ?? new List<int>();

            return await _context.SharedEventPaymentAllocations.AnyAsync(a =>
                (a.SharedEventMovementShareId != null && shareIds.Contains(a.SharedEventMovementShareId.Value)) ||
                (a.SharedExpenseSplitId != null && splitIds.Contains(a.SharedExpenseSplitId.Value)));
        }

        public async Task RemoveSharesAsync(IEnumerable<SharedEventMovementShare> shares)
        {
            _context.SharedEventMovementShares.RemoveRange(shares);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsTransactionReferencedAsync(int transactionId)
        {
            if (await _context.SharedEventMovements.AnyAsync(m => m.TransactionId == transactionId))
                return true;

            return await _context.SharedEventPaymentAllocations.AnyAsync(a =>
                a.CreatedExpenseTransactionId == transactionId ||
                a.CreatedIncomeTransactionId == transactionId ||
                a.CreatedExchangeOutTransactionId == transactionId ||
                a.CreatedExchangeInTransactionId == transactionId);
        }

        public async Task<bool> IsCardTransactionReferencedAsync(int cardTransactionId)
        {
            return await _context.SharedEventMovements.AnyAsync(m => m.CardTransactionId == cardTransactionId);
        }
    }
}
