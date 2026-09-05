using JazFinanzasApp.API.Business.DTO.CardReport;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ICardReportService
    {
        Task<CardGeneralReportDTO> GetGeneralAsync(int userId);
        Task<CardDetailReportDTO> GetByCardAsync(int userId, int cardId);
        Task<CardFutureCommitmentDTO> GetFutureCommitmentAsync(int userId);
        Task<CardPromotionsReportDTO> GetPromotionsAsync(int userId);
    }
}
