using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardTransactionDiscountRepository : IGenericRepository<CardTransactionDiscount>
    {
        Task<CardTransactionDiscount?> GetByCardTransactionIdAsync(int cardTransactionId);
        Task<IEnumerable<CardTransactionDiscount>> GetActiveByUserIdAsync(int userId);
        Task AddInstallmentAsync(CardTransactionDiscountInstallment installment);
        Task<IEnumerable<CardTransactionDiscountInstallment>> GetInstallmentsByDiscountIdAsync(int discountId);
        Task DeleteInstallmentAsync(int id);
    }
}
