using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ISharedEventMovementRepository : IGenericRepository<SharedEventMovement>
    {
        Task<SharedEventMovement?> GetDetailByIdAsync(int id);
        Task<bool> HasActivityAsync(int movementId);
        Task RemoveSharesAsync(IEnumerable<SharedEventMovementShare> shares);
        Task<bool> IsTransactionReferencedAsync(int transactionId);
        Task<bool> IsCardTransactionReferencedAsync(int cardTransactionId);
        Task<SharedEventMovementShare?> GetShareByIdAsync(int id);
        Task UpdateShareAsync(SharedEventMovementShare share);
    }
}
