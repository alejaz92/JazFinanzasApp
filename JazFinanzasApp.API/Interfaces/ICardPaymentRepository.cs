using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface ICardPaymentRepository : IGenericRepository<CardPayment>
    {
        Task<bool> IsPaymentAlreadyMadeAsync(int cardId, DateTime date);
    }
}
