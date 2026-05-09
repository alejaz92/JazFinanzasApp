using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardTransactionRepository : IGenericRepository<CardTransaction>
    {
        Task<IEnumerable<CardGraphResult>> GetCardStats(int? cardId, string Asset, int userId);
        Task<IEnumerable<CardTransaction>> GetCardTransactionsToPay(int cardId, DateTime paymentMonth, int userId);
        Task<IEnumerable<CardTransactionPendingResult>> GetPendingCardTransactionsAsync(int userId);
    }
}
