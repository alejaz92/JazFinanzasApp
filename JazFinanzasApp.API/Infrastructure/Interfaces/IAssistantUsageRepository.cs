using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAssistantUsageRepository
    {
        // Null si el usuario no tiene fila: equivale a deshabilitado (plan-devoluciones-llm.md, D12).
        Task<AssistantUserSettings?> GetSettingsAsync(int userId);

        // Suma CostUsd de las llamadas creadas en [from, to), sin contar las de evaluación (D11).
        Task<decimal> SumCostExcludingEvalAsync(int userId, DateTime from, DateTime to);

        // Guarda de inmediato: el registro tiene que sobrevivir a una falla posterior de la pasada.
        Task AddApiCallAsync(AssistantApiCall call);
    }
}
