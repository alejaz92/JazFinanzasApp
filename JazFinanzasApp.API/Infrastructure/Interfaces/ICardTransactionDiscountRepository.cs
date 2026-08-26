using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardTransactionDiscountRepository : IGenericRepository<CardTransactionDiscount>
    {
        Task<CardTransactionDiscount?> GetByCardTransactionIdAsync(int cardTransactionId);
        Task<IEnumerable<CardTransactionDiscount>> GetActiveByUserIdAsync(int userId);

        // Descuentos con saldo a favor todavia en la tarjeta, del mas viejo al mas nuevo:
        // ese es el orden en que el banco los consume.
        Task<IEnumerable<CardTransactionDiscount>> GetPendingOnCardAsync(int cardId, int userId);
        Task AddInstallmentAsync(CardTransactionDiscountInstallment installment);
        Task<IEnumerable<CardTransactionDiscountInstallment>> GetInstallmentsByDiscountIdAsync(int discountId);
        Task DeleteInstallmentAsync(int id);
    }
}
