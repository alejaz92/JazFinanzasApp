using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IInvestmentTransactionService
    {
        // Stocks
        Task CreateStockTransactionAsync(int userId, StockTransactionAddDTO dto);
        Task<(IEnumerable<StockTransactionListDTO> Transactions, int TotalCount)> GetPaginatedStockTransactionsAsync(int userId, int page, int pageSize, string environment);
        Task DeleteStockTransactionAsync(int userId, int id);

        // Crypto
        Task CreateCryptoTransactionAsync(int userId, InvestmentTransactionAddDTO dto);
        Task<(IEnumerable<CryptoTransactionListDTO> Transactions, int TotalCount)> GetPaginatedCryptoTransactionsAsync(int userId, int page, int pageSize);
        Task DeleteCryptoTransactionAsync(int userId, int id);

        // Portfolio transfers
        Task CreatePortfolioTransactionAsync(int userId, PortfolioTransactionAddDTO dto);
        Task<(IEnumerable<CurrencyExchangeListDTO> Transactions, int TotalCount)> GetPaginatedPortfolioTransactionsAsync(int userId, int page, int pageSize);
        Task DeletePortfolioTransactionAsync(int userId, int id);
    }
}
