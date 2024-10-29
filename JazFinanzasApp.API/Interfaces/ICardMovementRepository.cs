using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.CardMovement;

namespace JazFinanzasApp.API.Interfaces
{
    public interface ICardMovementRepository : IGenericRepository<CardMovement>
    {
        Task<IEnumerable<CardMovement>> GetCardMovementsToPay(int cardId, DateTime paymentMonth, int userId);
        Task<IEnumerable<CardMovementsPendingDTO>> GetPendingCardMovementsAsync(int userId);
    }
}
