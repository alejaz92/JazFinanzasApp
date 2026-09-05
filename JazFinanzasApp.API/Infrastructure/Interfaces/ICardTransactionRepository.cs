using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardTransactionRepository : IGenericRepository<CardTransaction>
    {
        Task<IEnumerable<CardGraphResult>> GetCardStats(int? cardId, string Asset, int userId);
        Task<IEnumerable<CardTransaction>> GetCardTransactionsToPay(int cardId, DateTime paymentMonth, int userId);
        Task<IEnumerable<CardTransactionPendingResult>> GetPendingCardTransactionsAsync(int userId);
        Task<IEnumerable<CardTransaction>> GetCardTransactionsByTripIdAsync(int tripId);
        Task<IEnumerable<CardTransaction>> GetTripOwnExpenseCardTransactionsAsync(int tripId);
        Task<IEnumerable<CardTransaction>> GetTripSuggestibleCardTransactionsAsync(int userId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<CardTransaction>> SearchTripAssociableCardTransactionsAsync(int userId, string? search);

        // Fase 14 (Tarjetas): todos los consumos del usuario, con Card/Asset/TransactionClass ya
        // incluidos — evita resolver cada navegación a mano como hace T8 en NetWorthReportService.
        Task<IEnumerable<CardTransaction>> GetByUserIdWithDetailsAsync(int userId);
    }
}
