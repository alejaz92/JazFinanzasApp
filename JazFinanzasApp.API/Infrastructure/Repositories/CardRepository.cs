using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class CardRepository : GenericRepository<Card>, ICardRepository
    {
        private readonly ApplicationDbContext _context;

        public CardRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }
        public async Task<bool> IsCardUsed(int cardId)
        {
            return await _context.CardTransactions.AnyAsync(c => c.CardId == cardId);
        }

    }
}
