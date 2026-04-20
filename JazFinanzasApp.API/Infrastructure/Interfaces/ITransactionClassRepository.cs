using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ITransactionClassRepository : IGenericRepository<TransactionClass>
    {
        Task<TransactionClass> GetTransactionClassByDescriptionAsync(string Description, int UserId);
        Task<bool> IsTransactionClassInUseAsync(int transactionClassId);
        Task<IEnumerable<TransactionClass>> GetByUserIdAsync(int userId);
    }
}
