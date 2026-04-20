using JazFinanzasApp.API.Business.DTO.CardTransaction;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ICardTransactionService
    {
        Task AddCardTransactionAsync(int userId, CardTransactionAddDTO dto);
        Task<IEnumerable<CardTransactionsPendingDTO>> GetPendingCardTransactionsAsync(int userId);
        Task<IEnumerable<CardTransactionPaymentListDTO>> GetCardPaymentsAsync(int userId, int cardId, DateTime paymentMonth);
        Task RegisterCardPaymentAsync(int userId, CardTransactionPaymentDTO dto);
        Task<EditRecurrentListDTO> GetRecurrentTransactionAsync(int userId, int id);
        Task UpdateRecurrentTransactionAsync(int userId, int id, EditRecurrentDTO dto);
        Task DeleteCardTransactionAsync(int userId, int id);
    }
}
