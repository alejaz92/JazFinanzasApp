using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ISharedEventRepository : IGenericRepository<SharedEvent>
    {
        Task<IEnumerable<SharedEvent>> GetByUserIdAsync(int userId, bool includeClosed);
        Task<SharedEvent?> GetDetailByIdAsync(int id);

        // Carga liviana: evento + participantes, sin movimientos/pagos/splits/allocations.
        // Usar en vez de GetDetailByIdAsync cuando solo hace falta validar dueño/cerrado/participantes
        // (evita recargar el grafo completo del evento, que se vuelve lento con muchos movimientos).
        Task<SharedEvent?> GetWithParticipantsAsync(int id);
        Task<SharedEventParticipant?> GetParticipantAsync(int sharedEventId, int personId);
        Task AddParticipantAsync(SharedEventParticipant participant);
        Task RemoveParticipantAsync(SharedEventParticipant participant);
        Task<bool> HasMovementsOrPaymentsAsync(int sharedEventId);
        Task<bool> ParticipantHasActivityAsync(int sharedEventId, int personId);
        Task DeleteEventWithParticipantsAsync(int sharedEventId);
        Task<List<SharedEvent>> GetOpenEventsDetailAsync(int userId);
        Task<List<SharedEvent>> GetDetailByTripIdAsync(int tripId);
    }
}
