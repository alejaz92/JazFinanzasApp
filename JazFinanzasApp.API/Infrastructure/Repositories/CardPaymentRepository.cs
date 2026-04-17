using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Domain;
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
    }
}
