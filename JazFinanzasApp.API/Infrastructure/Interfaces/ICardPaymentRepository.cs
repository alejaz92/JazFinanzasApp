using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardPaymentRepository : IGenericRepository<CardPayment>
    {
        Task<bool> IsPaymentAlreadyMadeAsync(int cardId, DateTime date);
    }
}
