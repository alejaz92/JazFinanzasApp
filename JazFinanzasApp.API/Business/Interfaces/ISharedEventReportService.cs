using JazFinanzasApp.API.Business.DTO.SharedEventReport;

namespace JazFinanzasApp.API.Business.Interfaces
{
    // Bloque E, Fase 21 (docs/plans/activos/plan-rediseno-reportes-v2.md): backend de la categoría
    // Compartidos (Flujo 7). Sin assetId explícito -- a diferencia del resto de las categorías, acá
    // no hay una única moneda de referencia: cada saldo se muestra en la suya (ver comentario de
    // SharedEventReportDTOs).
    public interface ISharedEventReportService
    {
        Task<SharedEventGeneralReportDTO> GetGeneralAsync(int userId);
        Task<SharedEventPersonReportDTO> GetByPersonAsync(int userId, int personId);
    }
}
