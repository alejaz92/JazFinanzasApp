using JazFinanzasApp.API.Business.DTO.CardReport;
using JazFinanzasApp.API.Business.DTO.CardTransaction;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ICardReportService
    {
        // Corrección 2026-09-05: assetId en los 4 reportes — moneda de referencia en la que se
        // expresan los montos (T12, la misma del selector de moneda de la barra de Reportes). Antes
        // solo General lo tenía.
        Task<CardGeneralReportDTO> GetGeneralAsync(int userId, int assetId);
        Task<CardDetailReportDTO> GetByCardAsync(int userId, int cardId, int assetId);
        Task<CardFutureCommitmentDTO> GetFutureCommitmentAsync(int userId, int assetId);
        Task<CardPromotionsReportDTO> GetPromotionsAsync(int userId, int assetId);

        // General, corrección 2026-09-05: el resumen del mes ahora se puede pedir para cualquier
        // mes, no solo el actual (que sigue viniendo embebido en GetGeneralAsync).
        Task<List<CardTransactionPaymentListDTO>> GetMonthSummaryAsync(int userId, DateTime month);
    }
}
