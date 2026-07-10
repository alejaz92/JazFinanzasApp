using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class TripSuggestionDismissalRepository : GenericRepository<TripSuggestionDismissal>, ITripSuggestionDismissalRepository
    {
        private readonly ApplicationDbContext _context;

        public TripSuggestionDismissalRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TripSuggestionDismissal>> GetByTripIdAsync(int tripId)
        {
            return await _context.TripSuggestionDismissals
                .Where(d => d.TripId == tripId)
                .ToListAsync();
        }

        public async Task<TripSuggestionDismissal?> GetByTripAndMovementAsync(int tripId, int? transactionId, int? cardTransactionId)
        {
            return await _context.TripSuggestionDismissals
                .FirstOrDefaultAsync(d => d.TripId == tripId
                    && d.TransactionId == transactionId
                    && d.CardTransactionId == cardTransactionId);
        }

        public async Task DeleteByTransactionIdAsync(int transactionId)
        {
            var dismissals = await _context.TripSuggestionDismissals
                .Where(d => d.TransactionId == transactionId)
                .ToListAsync();
            if (dismissals.Any())
            {
                _context.TripSuggestionDismissals.RemoveRange(dismissals);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByCardTransactionIdAsync(int cardTransactionId)
        {
            var dismissals = await _context.TripSuggestionDismissals
                .Where(d => d.CardTransactionId == cardTransactionId)
                .ToListAsync();
            if (dismissals.Any())
            {
                _context.TripSuggestionDismissals.RemoveRange(dismissals);
                await _context.SaveChangesAsync();
            }
        }
    }
}
