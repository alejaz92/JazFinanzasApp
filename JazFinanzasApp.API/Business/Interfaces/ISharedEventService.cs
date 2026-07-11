using JazFinanzasApp.API.Business.DTO.SharedEvent;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ISharedEventService
    {
        Task<IEnumerable<SharedEventListDTO>> GetAllForUserAsync(int userId, bool includeClosed);
        Task<SharedEventDTO> GetByIdAsync(int userId, int id);
        Task<SharedEventDTO> CreateAsync(int userId, SharedEventAddDTO dto);
        Task UpdateAsync(int userId, int id, SharedEventEditDTO dto);
        Task<SharedEventDTO> AddParticipantAsync(int userId, int id, SharedEventParticipantAddDTO dto);
        Task RemoveParticipantAsync(int userId, int id, int personId);
        Task CloseAsync(int userId, int id);
        Task ReopenAsync(int userId, int id);
        Task DeleteAsync(int userId, int id);
        Task<SharedEventMovementDTO> CreateMovementAsync(int userId, int sharedEventId, SharedEventMovementAddDTO dto);
        Task<SharedEventMovementDTO> UpdateMovementAsync(int userId, int sharedEventId, int movementId, SharedEventMovementAddDTO dto);
        Task DeleteMovementAsync(int userId, int sharedEventId, int movementId);
    }
}
