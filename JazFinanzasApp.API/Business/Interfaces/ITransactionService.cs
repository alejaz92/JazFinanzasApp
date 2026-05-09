using JazFinanzasApp.API.Business.DTO.Transaction;
using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ITransactionService
    {
        Task<(IEnumerable<TransactionListDTO> Transactions, int TotalCount)> GetPaginatedTransactionsAsync(int userId, int page, int pageSize);
        Task<TransactionListDTO> GetTransactionByIdAsync(int userId, int id);
        Task CreateTransactionAsync(int userId, TransactionAddDTO dto);
        Task EditTransactionAsync(int userId, int id, TransactionEditDTO dto);
        Task DeleteTransactionAsync(int userId, int id);
        Task RefundTransactionAsync(int userId, int id, RefundDTO dto);
        Task<(IEnumerable<CurrencyExchangeListDTO> Transactions, int TotalCount)> GetPaginatedExchangeTransactionsAsync(int userId, int page, int pageSize);
        Task DeleteExchangeTransactionAsync(int userId, int id);
    }
}
