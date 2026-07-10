using JazFinanzasApp.API.Business.DTO.Report;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<TotalsBalanceDTO>> GetTotalsBalanceAsync(int userId);
        Task<IEnumerable<BalanceDTO>> GetBalanceByAssetAsync(int userId, int assetId);
        Task<IncExpStatsDTO> GetIncExpStatsAsync(int userId, DateTime month, int assetId);
        Task<CardsStatsDTO> GetCardStatsAsync(int userId, int cardId);
        Task<StockStatsDTO> GetStockStatsAsync(int userId, int assetTypeId);
        Task<CryptoGralStatsDTO> GetCryptoGralStatsAsync(int userId, bool includeStables);
        Task<CryptoStatsDTO> GetCryptoStatsAsync(int userId, int assetId);
        Task<HomeStatsDTO> GetHomeStatsAsync(int userId);
        Task<IEnumerable<PortfolioStatsDTO>> GetPortfolioStatsAsync(int userId);
        Task<PortfolioDetailStatsDTO> GetPortfolioDetailStatsAsync(int userId, int portfolioId);
    }
}
