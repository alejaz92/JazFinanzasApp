using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ISharedExpenseRepository : IGenericRepository<SharedExpense>
    {
        Task<SharedExpense?> GetByTransactionIdAsync(int transactionId);
        Task<SharedExpense?> GetByCardTransactionIdAsync(int cardTransactionId);
        Task<SharedExpenseSplit?> GetSplitByIdAsync(int splitId);
        Task<IEnumerable<SharedExpenseSplit>> GetPendingSplitsByUserIdAsync(int userId);
        Task UpdateSplitAsync(SharedExpenseSplit split);
        Task DeleteByTransactionIdAsync(int transactionId);
        Task AddReimbursementAsync(SharedExpenseReimbursement reimbursement);
        Task<IEnumerable<SharedExpenseReimbursement>> GetReimbursementsBySplitIdAsync(int splitId);
        Task DeleteReimbursementAsync(int id);
        Task<bool> IsLinkedToSharedEventAsync(int sharedExpenseId);
        Task DeleteByIdWithSplitsAsync(int id);
    }
}
