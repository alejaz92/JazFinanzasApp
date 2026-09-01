using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetPaginatedTransactions(int userId, int page, int pageSize);
        Task<IEnumerable<BalanceResult>> GetBalanceByAssetAndUserAsync(int assetId, int userId);
        Task<TotalsBalanceResult> GetTotalsBalanceByUserAsync(int userId, Asset asset);
        Task<IncExpResult> GetDollarIncExpStatsAsync(int userId, DateTime month);
        Task<IncExpResult> GetPesosIncExpStatsAsync(int userId, DateTime month);
        Task<IEnumerable<StockStatsListResult>> GetStockStatsAsync(int userId, int assetTypeId, string environment, bool considerStable,
            int referenceAssetId);
        Task<IEnumerable<StocksGralStatsResult>> GetStocksGralStatsAsync(int userId, string environment, int referenceAssetId);
        Task<IEnumerable<CryptoStatsByDateResult>> GetCryptoStatsByDateAsync(int userId, int assetTypeId, string environment, int? assetId, bool considerStable, int referenceAssetId);
        Task<IEnumerable<CryptoStatsByDateCommerceResult>> GetInvestmentsHoldingsStats(int userId, int assetTypeId, string environment, int? assetId, bool considerStable, int months, int referenceId);
        Task<IEnumerable<InvestmentTransactionsResult>> GetInvestmentsTransactionsStats(int userId, int assetId, int referenceAssetId);
        Task<IncExpResult> GetIncExpStatsAsync(int userId, DateTime month, Asset asset);
        Task<decimal> GetAverageBuyValue(int userId, int assetId, int referenceAssetId);
        Task<decimal> GetBalance(int accountId, int assetId, int portfolioId);
        Task<decimal> GetAverageQuotePrice(int accountId, int assetId, int portfolioId);
        Task<IEnumerable<PortfolioStatsResult>> GetPortfolioStatsAsync(int userId, int referenceAssetId);
        Task<IEnumerable<PortfolioHoldingResult>> GetPortfolioHoldingsAsync(int userId, int portfolioId, int referenceAssetId);
        Task<IEnumerable<PortfolioValueByDateResult>> GetPortfolioValueByDateAsync(int userId, int portfolioId, int referenceAssetId, int months);
        Task<IEnumerable<Transaction>> GetTransactionsByTripIdAsync(int tripId);
        Task<IEnumerable<Transaction>> GetTripOwnExpenseTransactionsAsync(int tripId);
        Task<IEnumerable<Transaction>> GetTripSuggestibleTransactionsAsync(int userId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<Transaction>> SearchTripAssociableTransactionsAsync(int userId, string? search);
        Task<IEnumerable<Transaction>> GetByCardTransactionIdAsync(int cardTransactionId);
        Task DetachConsumedIncomeFromSharedEventPaymentAllocationsAsync(int transactionId);

        // Patrimonio (Fase 10) — no tocan GetTotalsBalanceByUserAsync (T7).
        Task<(decimal Rate, DateTime? QuoteDate)> GetReferenceAssetRateAsync(Asset asset);
        Task<DateTime?> GetOldestQuoteDateForHoldingsAsync(int userId);
        Task<IEnumerable<NetWorthMonthlyPointResult>> GetNetWorthMonthlySeriesAsync(int userId, Asset referenceAsset, int months);
        Task<IEnumerable<AccountBalanceResult>> GetAccountBalancesAsync(int userId, Asset referenceAsset, int evolutionMonths);
        Task<IEnumerable<CurrencyExposureResult>> GetCurrencyExposureAsync(int userId, Asset referenceAsset);
        Task<IEnumerable<MonthlyBalanceResult>> GetDollarizedPercentSeriesAsync(int userId, int months);
    }
}
