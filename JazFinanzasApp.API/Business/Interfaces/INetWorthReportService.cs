using JazFinanzasApp.API.Business.DTO.NetWorth;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface INetWorthReportService
    {
        Task<IEnumerable<NetWorthTotalDTO>> GetGeneralAsync(int userId);
        Task<IEnumerable<NetWorthMonthlyPointDTO>> GetMonthlySeriesAsync(int userId, int assetId);
        Task<IEnumerable<AccountBalanceDTO>> GetByAccountAsync(int userId, int assetId);
        Task<IEnumerable<CurrencyExposureDTO>> GetByCurrencyAsync(int userId, int assetId);
        Task<IEnumerable<MonthlyBalanceDTO>> GetDollarizedPercentSeriesAsync(int userId);
        Task<decimal> GetLiveCardDebtInDollarsAsync(int userId);
    }
}
