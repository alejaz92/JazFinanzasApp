using JazFinanzasApp.API.Business.DTO.SharedExpense;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ISharedExpenseService
    {
        Task<SharedExpenseDetailDTO> CreateAsync(int userId, SharedExpenseAddDTO dto);
        Task<SharedExpenseDetailDTO> GetByTransactionIdAsync(int userId, int transactionId);
        Task<SharedExpenseDetailDTO> GetByCardTransactionIdAsync(int userId, int cardTransactionId);
        Task<SharedExpenseSplitDTO> RegisterReimbursementAsync(int userId, RegisterReimbursementDTO dto);
        Task DeleteAsync(int userId, int id);
        Task<IEnumerable<PersonDebtSummaryDTO>> GetSummaryAsync(int userId);
    }
}
