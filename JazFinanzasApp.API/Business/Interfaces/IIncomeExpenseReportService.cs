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

        // Ingresos (corrección 2026-09-04 sobre la Fase 13).
        Task<IEnumerable<IncomeCategorySeriesDTO>> GetIncomeByCategoryAsync(int userId, int assetId, int months);
        Task<PayDayCalendarDTO> GetPayDaysAsync(int userId, int assetId, int months);
    }
}
