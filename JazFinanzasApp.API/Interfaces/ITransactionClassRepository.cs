using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface ITransactionClassRepository : IGenericRepository<TransactionClass>
    {
        Task<TransactionClass> GetTransactionClassByDescriptionAsync(string Description);
    }
}
