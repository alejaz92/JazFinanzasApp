using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Repositories
{
    public class CardRepository : GenericRepository<Card>, ICardRepository
    {
        private readonly ApplicationDbContext _context;

        public CardRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }

        // check if card is used in card transactions
        public bool IsCardUsed(int cardId)
        {
            return _context.CardTransactions.Find(cardId) != null;
        }
        
    }
}
