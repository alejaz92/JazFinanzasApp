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

        // Asignaciones de pago que saldaron la parte propia de un movimiento de Evento (SharedEventMovementShareId
        // != null) tocando o creando una de las transacciones dadas — el único rastro cuando el movimiento nunca
        // tuvo TransactionId/CardTransactionId propio (lo pagó otra persona y el usuario saldó después).
        Task<List<SharedEventPaymentAllocation>> GetSettlementAllocationsByTransactionIdsAsync(IEnumerable<int> transactionIds);
    }
}
