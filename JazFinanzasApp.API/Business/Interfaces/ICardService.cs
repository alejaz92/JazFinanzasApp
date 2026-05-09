using JazFinanzasApp.API.Business.DTO.Card;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ICardService
    {
        Task<IEnumerable<CardDTO>> GetAllForUserAsync(int userId);
        Task<CardDTO> GetByIdAsync(int userId, int id);
        Task CreateCardAsync(int userId, CardDTO dto);
        Task UpdateCardAsync(int userId, int id, CardDTO dto);
        Task DeleteCardAsync(int userId, int id);
    }
}
