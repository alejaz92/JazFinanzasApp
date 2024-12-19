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
        Task<IncExpStatsDTO> GetDollarIncExpStatsAsync(int userId, DateTime month);
        Task<IncExpStatsDTO> GetPesosIncExpStatsAsync(int userId, DateTime month);
        Task<IEnumerable<StockStatsListDTO>> GetStockStatsAsync(int userId, int assetTypeId, string environment, bool considerStable);
        Task<IEnumerable<StocksGralStatsDTO>> GetStocksGralStatsAsync(int userId, string environment);
        Task<IEnumerable<CryptoStatsByDateDTO>> GetCryptoStatsByDateAsync(int userId, int assetTypeId, string environment, int? assetId, bool considerStable);
        Task<IEnumerable<CryptoStatsByDateCommerceDTO>> GetCryptoStatsTransactionStats(int userId, int assetTypeId, string environment, int? assetId, bool considerStable, int months);
    }
}
