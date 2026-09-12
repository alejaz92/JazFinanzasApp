using JazFinanzasApp.API.Business.DTO.TripReport;

namespace JazFinanzasApp.API.Business.Interfaces
{
    // Bloque E, Fase 21 (docs/plans/activos/plan-rediseno-reportes-v2.md): backend de la categoría
    // Viajes (Flujo 6). assetId es siempre la moneda de referencia elegida en la barra de Reportes
    // (T12) — mismo criterio que IInvestmentReportService/ICardReportService.
    public interface ITripReportService
    {
        Task<IEnumerable<TripGeneralReportDTO>> GetGeneralAsync(int userId, int assetId);
        Task<TripDetailReportDTO> GetDetailAsync(int userId, int tripId, int assetId);
    }
}
