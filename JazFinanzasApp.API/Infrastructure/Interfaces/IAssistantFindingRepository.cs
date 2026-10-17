using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    // Acceso a los hallazgos del asistente (plan-devoluciones-llm.md, D8b y D15). Las entidades vuelven con
    // seguimiento: el servicio las modifica y llama a SaveChangesAsync.
    public interface IAssistantFindingRepository
    {
        // Los hallazgos de un usuario, del más nuevo al más viejo, sin los Repeated (que no se muestran).
        // onlyActive: solo los que están Active.
        Task<List<AssistantFinding>> GetByUserAsync(int userId, bool onlyActive);

        // Solo si es del usuario y no es un Repeated.
        Task<AssistantFinding?> GetByIdAsync(int userId, int id);

        // Los Fingerprint que el usuario ya tiene desde esa fecha, en cualquier estado (activo, descartado,
        // vencido o repetido): un hallazgo descartado no vuelve en el mismo mes (D15).
        Task<HashSet<string>> GetFingerprintsSinceAsync(int userId, DateTime since);

        Task AddRangeAsync(IEnumerable<AssistantFinding> findings);
        Task SaveChangesAsync();
    }
}
