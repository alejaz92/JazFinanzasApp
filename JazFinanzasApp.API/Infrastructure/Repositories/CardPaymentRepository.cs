using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class CardPaymentRepository : GenericRepository<CardPayment>, ICardPaymentRepository
    {
        private readonly ApplicationDbContext _context;
        public CardPaymentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> IsPaymentAlreadyMadeAsync(int cardId, DateTime date)
        {
            return await _context.CardPayments
                .AnyAsync(cp => cp.CardId == cardId 
                    && cp.Date.Month == date.Month
                    && cp.Date.Year == date.Year);
        }

        public async Task<IEnumerable<DateTime>> GetPaidMonthsAsync(int cardId)
        {
            return await _context.CardPayments
                .Where(cp => cp.CardId == cardId)
                .Select(cp => cp.Date)
                .ToListAsync();
        }

        public async Task<Dictionary<int, DateTime>> GetLastPaidMonthByCardAsync(int userId)
        {
            return await _context.CardPayments
                .Where(cp => cp.Card.UserId == userId)
                .GroupBy(cp => cp.CardId)
                .Select(g => new { CardId = g.Key, LastPaid = g.Max(cp => cp.Date) })
                .ToDictionaryAsync(x => x.CardId, x => x.LastPaid);
        }
    }
}
