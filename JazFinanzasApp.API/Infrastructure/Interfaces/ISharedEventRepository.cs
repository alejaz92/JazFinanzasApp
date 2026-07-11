using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ISharedEventRepository : IGenericRepository<SharedEvent>
    {
        Task<IEnumerable<SharedEvent>> GetByUserIdAsync(int userId, bool includeClosed);
        Task<SharedEvent?> GetDetailByIdAsync(int id);
        Task<SharedEventParticipant?> GetParticipantAsync(int sharedEventId, int personId);
        Task AddParticipantAsync(SharedEventParticipant participant);
        Task RemoveParticipantAsync(SharedEventParticipant participant);
        Task<bool> HasMovementsOrPaymentsAsync(int sharedEventId);
        Task<bool> ParticipantHasActivityAsync(int sharedEventId, int personId);
        Task DeleteEventWithParticipantsAsync(int sharedEventId);
    }
}
