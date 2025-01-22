using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
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
