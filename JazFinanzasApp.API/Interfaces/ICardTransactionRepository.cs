using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.CardTransaction;
using JazFinanzasApp.API.Models.DTO.Report;

namespace JazFinanzasApp.API.Interfaces
{
    public interface ICardTransactionRepository : IGenericRepository<CardTransaction>
    {
        Task<IEnumerable<CardGraphDTO>> GetCardStats(int? cardId, string Asset, int userId);
        Task<IEnumerable<CardTransaction>> GetCardTransactionsToPay(int cardId, DateTime paymentMonth, int userId);
        Task<IEnumerable<CardTransactionsPendingDTO>> GetPendingCardTransactionsAsync(int userId);
    }
}
