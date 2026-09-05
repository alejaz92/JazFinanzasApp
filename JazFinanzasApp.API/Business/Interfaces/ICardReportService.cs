using JazFinanzasApp.API.Business.DTO.CardReport;
using JazFinanzasApp.API.Business.DTO.CardTransaction;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ICardReportService
    {
        Task<CardGeneralReportDTO> GetGeneralAsync(int userId);
        Task<CardDetailReportDTO> GetByCardAsync(int userId, int cardId);
        Task<CardFutureCommitmentDTO> GetFutureCommitmentAsync(int userId);
        Task<CardPromotionsReportDTO> GetPromotionsAsync(int userId);

        // General, corrección 2026-09-05: el resumen del mes ahora se puede pedir para cualquier
        // mes, no solo el actual (que sigue viniendo embebido en GetGeneralAsync).
        Task<List<CardTransactionPaymentListDTO>> GetMonthSummaryAsync(int userId, DateTime month);
    }
}
