using JazFinanzasApp.API.Business.DTO.CardTransactionDiscount;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ICardTransactionDiscountService
    {
        Task<CardTransactionDiscountDetailDTO> CreateAsync(int userId, CardTransactionDiscountAddDTO dto);
        Task<CardTransactionDiscountDetailDTO> GetByCardTransactionIdAsync(int userId, int cardTransactionId);
        Task DeleteAsync(int userId, int id);
    }
}
