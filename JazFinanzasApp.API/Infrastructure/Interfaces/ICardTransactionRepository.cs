using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.DTO.Report;
using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardTransactionRepository : IGenericRepository<CardTransaction>
    {
        Task<IEnumerable<CardGraphDTO>> GetCardStats(int? cardId, string Asset, int userId);
        Task<IEnumerable<CardTransaction>> GetCardTransactionsToPay(int cardId, DateTime paymentMonth, int userId);
        Task<IEnumerable<CardTransactionsPendingDTO>> GetPendingCardTransactionsAsync(int userId);
    }
}
