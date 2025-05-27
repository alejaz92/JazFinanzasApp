using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ITransactionClassRepository : IGenericRepository<TransactionClass>
    {
        Task<TransactionClass> GetTransactionClassByDescriptionAsync(string Description, int UserId);
        Task<bool> IsTransactionClassInUseAsync(int transactionClassId);
    }
}
