using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Report;

namespace JazFinanzasApp.API.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetPaginatedTransactions(int userId, int page, int pageSize);
        Task<IEnumerable<BalanceDTO>> GetBalanceByAssetAndUserAsync(int assetId, int userId);
        Task<IEnumerable<TotalsBalanceDTO>> GetTotalsBalanceByUserAsync(int userId);
    }
}
