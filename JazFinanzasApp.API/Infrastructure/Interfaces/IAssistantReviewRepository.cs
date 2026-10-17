using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    // Acceso a las pasadas del asistente (plan-devoluciones-llm.md, D6). Las entidades vuelven con
    // seguimiento: el servicio las modifica y llama a SaveChangesAsync.
    public interface IAssistantReviewRepository
    {
        // Solo si es del usuario.
        Task<AssistantReview?> GetByIdAsync(int userId, int id);

        // Las pasadas de un período, de la más nueva a la más vieja.
        Task<List<AssistantReview>> GetForPeriodAsync(int userId, string kind, DateTime periodStart, string purpose);

        // Devoluciones vigentes de un usuario (propósito User, MonthClose o MidMonth), de período más nuevo a
        // más viejo, con filtro opcional por tipo.
        Task<List<AssistantReview>> GetCurrentUserReviewsAsync(int userId, string? kind);

        Task<AssistantReview?> GetLatestReadyAsync(int userId);

        // Cuántos movimientos tuvo el usuario entre dos fechas, inclusive: transacciones y consumos de tarjeta.
        Task<bool> HasActivityAsync(int userId, DateTime from, DateTime to);

        // Las pasadas del usuario (propósito User, cualquier tipo) creadas desde esa fecha, sin seguimiento.
        Task<List<AssistantReview>> GetUserPassesSinceAsync(int userId, DateTime since);

        // Cuándo se hizo la última pasada terminada del usuario (Ready, cualquier tipo), o null si no hubo.
        Task<DateTime?> GetLastReadyPassTimeAsync(int userId);

        // Si el usuario cargó algún movimiento (transacción o consumo de tarjeta) después de esa fecha. Mira cuándo
        // se cargó, no la fecha del movimiento.
        Task<bool> HasMovementsLoadedSinceAsync(int userId, DateTime since);

        Task<Dictionary<int, AssistantFeedback>> GetFeedbackByReviewIdsAsync(IEnumerable<int> reviewIds);

        Task AddAsync(AssistantReview review);
        Task AddFeedbackAsync(AssistantFeedback feedback);
        Task SaveChangesAsync();
    }
}
