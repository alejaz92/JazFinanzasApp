using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface ICardRepository : IGenericRepository<Card>
    {
        Task<bool> IsCardUsed(int cardId);
    }
}
