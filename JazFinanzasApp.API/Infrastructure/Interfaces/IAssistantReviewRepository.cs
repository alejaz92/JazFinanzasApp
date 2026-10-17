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

        Task<Dictionary<int, AssistantFeedback>> GetFeedbackByReviewIdsAsync(IEnumerable<int> reviewIds);

        Task AddAsync(AssistantReview review);
        Task AddFeedbackAsync(AssistantFeedback feedback);
        Task SaveChangesAsync();
    }
}
