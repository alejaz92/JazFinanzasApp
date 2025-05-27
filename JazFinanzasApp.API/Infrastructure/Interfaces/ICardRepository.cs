using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardRepository : IGenericRepository<Card>
    {
        Task<bool> IsCardUsed(int cardId);
    }
}
