using JazFinanzasApp.API.Business.DTO.CardTransactionDiscount;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ICardTransactionDiscountService
    {
        Task<CardTransactionDiscountDetailDTO> CreateAsync(int userId, CardTransactionDiscountAddDTO dto);

        // Convierte parte de un descuento en plata dentro de una cuenta, repartiendola entre las
        // cuotas todavia no pagadas. Ver plan-reintegro-saldo-tarjeta.md (D1/D2).
        Task MaterializeAsync(CardTransactionDiscount discount, decimal amount, int accountId, DateTime date, int userId);
        Task<CardTransactionDiscountDetailDTO> GetByCardTransactionIdAsync(int userId, int cardTransactionId);
        Task<IEnumerable<CardTransactionDiscountDetailDTO>> GetActiveByUserIdAsync(int userId);
        Task<CardTransactionDiscountDetailDTO> RescueAsync(int userId, int id, CardTransactionDiscountRescueDTO dto);
        Task<CardPendingCreditDTO> GetPendingOnCardAsync(int userId, int cardId);
        Task DeleteAsync(int userId, int id);
    }
}
