using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
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
