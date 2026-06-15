using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ISharedExpenseRepository : IGenericRepository<SharedExpense>
    {
        Task<SharedExpense?> GetByTransactionIdAsync(int transactionId);
        Task<IEnumerable<SharedExpenseSplit>> GetPendingSplitsByUserIdAsync(int userId);
        Task UpdateSplitAsync(SharedExpenseSplit split);
    }
}
