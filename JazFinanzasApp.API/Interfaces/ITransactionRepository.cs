using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Report;

namespace JazFinanzasApp.API.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetPaginatedTransactions(int userId, int page, int pageSize);
        Task<IEnumerable<BalanceDTO>> GetBalanceByAssetAndUserAsync(int assetId, int userId);
        Task<TotalsBalanceDTO> GetTotalsBalanceByUserAsync(int userId, Asset asset);
        Task<IncExpStatsDTO> GetDollarIncExpStatsAsync(int userId, DateTime month);
        Task<IncExpStatsDTO> GetPesosIncExpStatsAsync(int userId, DateTime month);
        Task<IEnumerable<StockStatsListDTO>> GetStockStatsAsync(int userId, int assetTypeId, string environment, bool considerStable,
            int referenceAssetId);
        Task<IEnumerable<StocksGralStatsDTO>> GetStocksGralStatsAsync(int userId, string environment, int referenceAssetId);
        Task<IEnumerable<CryptoStatsByDateDTO>> GetCryptoStatsByDateAsync(int userId, int assetTypeId, string environment, int? assetId, bool considerStable, int referenceAssetId);
        Task<IEnumerable<CryptoStatsByDateCommerceDTO>> GetInvestmentsHoldingsStats(int userId, int assetTypeId, string environment, int? assetId, bool considerStable, int months, int referenceId);
        Task<IEnumerable<InvestmentTransactionsStatsDTO>> GetInvestmentsTransactionsStats(int userId, int assetId, int referenceAssetId);
        Task<IncExpStatsDTO> GetIncExpStatsAsync(int userId, DateTime month, Asset asset);
        Task<decimal> GetAverageBuyValue(int userId, int assetId, int referenceAssetId);
    }
}
