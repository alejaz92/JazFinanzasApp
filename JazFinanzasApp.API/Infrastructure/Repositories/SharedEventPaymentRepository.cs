using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class SharedEventPaymentRepository : GenericRepository<SharedEventPayment>, ISharedEventPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public SharedEventPaymentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<SharedEventMovement>> GetMovementsWithPendingCreditsAsync(int sharedEventId, int assetId)
        {
            return await _context.SharedEventMovements
                .Where(m => m.SharedEventId == sharedEventId && m.AssetId == assetId && m.SharedExpenseId != null)
                .Include(m => m.SharedExpense)
                    .ThenInclude(se => se.Splits)
                        .ThenInclude(s => s.Person)
                .OrderBy(m => m.Date).ThenBy(m => m.Id)
                .ToListAsync();
        }

        public async Task<List<SharedEventMovement>> GetMovementsWithPendingDebtsAsync(int sharedEventId, int assetId)
        {
            return await _context.SharedEventMovements
                .Where(m => m.SharedEventId == sharedEventId && m.AssetId == assetId && m.PayerPersonId != null)
                .Include(m => m.Shares)
                .Include(m => m.PayerPerson)
                .OrderBy(m => m.Date).ThenBy(m => m.Id)
                .ToListAsync();
        }

        public async Task<SharedEventPayment?> GetDetailByIdAsync(int id)
        {
            return await _context.SharedEventPayments
                .Include(p => p.FromPerson)
                .Include(p => p.ToPerson)
                .Include(p => p.Asset)
                .Include(p => p.Account)
                .Include(p => p.Allocations)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<SharedEventPayment?> GetLastPaymentAsync(int sharedEventId)
        {
            return await _context.SharedEventPayments
                .Where(p => p.SharedEventId == sharedEventId)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        public async Task DeletePaymentWithAllocationsAsync(int paymentId)
        {
            var allocations = await _context.SharedEventPaymentAllocations
                .Where(a => a.SharedEventPaymentId == paymentId)
                .ToListAsync();
            _context.SharedEventPaymentAllocations.RemoveRange(allocations);

            var payment = await _context.SharedEventPayments.FindAsync(paymentId);
            if (payment != null)
                _context.SharedEventPayments.Remove(payment);

            await _context.SaveChangesAsync();
        }
    }
}
