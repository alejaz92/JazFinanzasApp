using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IIncomeExpenseReportService
    {
        Task<IncExpWaterfallDTO> GetWaterfallAsync(int userId, DateTime month, int assetId);
        Task<IEnumerable<IncExpEvolutionPointDTO>> GetEvolutionAsync(int userId, int assetId, int months);
        Task<SpendingByCategoryDTO> GetByCategoryAsync(int userId, DateTime month, int assetId);
        Task<IEnumerable<TagSpendingDTO>> GetByTagAsync(int userId, int assetId, int months);
        Task<SpendingCalendarDTO> GetCalendarAsync(int userId, int assetId, int year);

        // Asistente (plan-devoluciones-llm.md, D2): gasto por categoría del día 1 al `cutoffDay` del mes,
        // contra el mismo tramo del mes anterior y el promedio de los meses anteriores. Con `cutoffDay`
        // igual a la cantidad de días del mes es el mes completo; uno mayor se acota a esa cantidad.
        Task<SpendingToDateDTO> GetSpendingToDateByCategoryAsync(int userId, int assetId, DateTime month, int cutoffDay);

        // Ingresos (corrección 2026-09-04 sobre la Fase 13). GetIncomeCompositionAsync es la
        // principal (mes elegido); GetIncomeByCategoryAsync es la evolución, secundaria.
        Task<IncomeCompositionDTO> GetIncomeCompositionAsync(int userId, DateTime month, int assetId);
        Task<IEnumerable<IncomeCategorySeriesDTO>> GetIncomeByCategoryAsync(int userId, int assetId, int months);
        Task<IEnumerable<IncomeCategoryDayDTO>> GetIncomeByCategoryAndDayAsync(int userId, int assetId, int months);
    }
}
