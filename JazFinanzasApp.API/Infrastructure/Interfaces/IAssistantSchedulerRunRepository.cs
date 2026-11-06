using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAssistantSchedulerRunRepository
    {
        // Guarda de inmediato: la corrida tiene que quedar registrada aunque algo falle después.
        Task AddAsync(AssistantSchedulerRun run);

        Task SaveChangesAsync();

        // La corrida más reciente (por inicio), o null si nunca hubo una.
        Task<AssistantSchedulerRun?> GetLastAsync();

        // Corridas que empezaron en [from, ahora) y siguen en Running.
        Task<bool> AnyRunningSinceAsync(DateTime from);
    }
}
