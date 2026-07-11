using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ISharedEventPaymentRepository : IGenericRepository<SharedEventPayment>
    {
        // Movimientos pagados por el usuario (con SharedExpense) en la moneda dada — de ahí salen los ítems "a favor" (C)
        Task<List<SharedEventMovement>> GetMovementsWithPendingCreditsAsync(int sharedEventId, int assetId);

        // Movimientos pagados por un tercero en la moneda dada — de ahí sale la deuda propia del usuario (D)
        Task<List<SharedEventMovement>> GetMovementsWithPendingDebtsAsync(int sharedEventId, int assetId);

        Task<SharedEventPayment?> GetDetailByIdAsync(int id);
        Task<SharedEventPayment?> GetLastPaymentAsync(int sharedEventId);
        Task DeletePaymentWithAllocationsAsync(int paymentId);
    }
}
