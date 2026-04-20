using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardRepository : IGenericRepository<Card>
    {
        Task<bool> IsCardUsed(int cardId);
        Task<IEnumerable<Card>> GetByUserIdAsync(int userId);
    }
}
