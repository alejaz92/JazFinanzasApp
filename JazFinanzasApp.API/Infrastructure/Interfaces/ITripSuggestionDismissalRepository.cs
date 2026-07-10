using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ITripSuggestionDismissalRepository : IGenericRepository<TripSuggestionDismissal>
    {
        Task<IEnumerable<TripSuggestionDismissal>> GetByTripIdAsync(int tripId);
        Task<TripSuggestionDismissal?> GetByTripAndMovementAsync(int tripId, int? transactionId, int? cardTransactionId);
        Task DeleteByTransactionIdAsync(int transactionId);
        Task DeleteByCardTransactionIdAsync(int cardTransactionId);
    }
}
