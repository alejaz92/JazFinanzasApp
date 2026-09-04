using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetPaginatedTransactions(int userId, int page, int pageSize,
            int? classId = null, int? tagId = null, DateTime? from = null, DateTime? to = null);
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
        Task<IEnumerable<StaleAssetResult>> GetStaleAssetsAsync(int userId, int staleDaysThreshold);
        Task<IEnumerable<NetWorthMonthlyPointResult>> GetNetWorthMonthlySeriesAsync(int userId, Asset referenceAsset, int months);
        Task<IEnumerable<AccountBalanceResult>> GetAccountBalancesAsync(int userId, Asset referenceAsset, int evolutionMonths);

        // Ingresos y Egresos (Fase 12) — mismas guardas T1/T2 que GetIncExpStatsAsync, sin tocarlo.
        Task<IncExpWaterfallResult> GetIncExpWaterfallAsync(int userId, DateTime month, Asset asset);
        Task<IEnumerable<IncExpEvolutionPointResult>> GetIncExpEvolutionAsync(int userId, Asset asset, int months);
        Task<IEnumerable<CategorySpendingResult>> GetSpendingByCategoryMonthlySeriesAsync(int userId, Asset asset, DateTime month, int months);
        Task<IEnumerable<TagSpendingResult>> GetSpendingByTagAsync(int userId, Asset asset, int months);
        Task<IEnumerable<DailySpendingResult>> GetDailySpendingAsync(int userId, Asset asset, int year);

        // Ingresos (corrección 2026-09-04 sobre la Fase 13): evolución por categoría en vez de
        // composición de un mes — sueldo/aporte familiar explican el 90% del ingreso (1.2 del plan),
        // así que una foto de un mes no dice nada; en el tiempo sí (aumentos, aguinaldo, extra).
        Task<IEnumerable<IncomeCategorySeriesResult>> GetIncomeByCategoryMonthlySeriesAsync(int userId, Asset asset, int months);
        Task<IEnumerable<DailySpendingResult>> GetDailyIncomeAsync(int userId, Asset asset, int months);
    }
}
