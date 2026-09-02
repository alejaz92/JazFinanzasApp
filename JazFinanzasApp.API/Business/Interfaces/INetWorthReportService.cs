using JazFinanzasApp.API.Business.DTO.NetWorth;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface INetWorthReportService
    {
        Task<NetWorthGeneralDTO> GetGeneralAsync(int userId);
        Task<IEnumerable<NetWorthMonthlyPointDTO>> GetMonthlySeriesAsync(int userId, int assetId);
        Task<IEnumerable<AccountBalanceDTO>> GetByAccountAsync(int userId, int assetId);
        Task<decimal> GetLiveCardDebtInDollarsAsync(int userId);
    }
}
