using JazFinanzasApp.API.Business.DTO.InvestmentReport;

namespace JazFinanzasApp.API.Business.Interfaces
{
    // Bloque E, Fase 19 (docs/plans/activos/plan-rediseno-reportes-v2.md): backend de la categoría
    // Inversiones (Flujo 5). assetId es siempre la moneda de referencia elegida en la barra de
    // Reportes (T12) — mismo criterio que INetWorthReportService/ICardReportService.
    public interface IInvestmentReportService
    {
        Task<InvestmentOverviewDTO> GetOverviewAsync(int userId, int assetId);

        Task<PortfoliosOverviewDTO> GetPortfoliosOverviewAsync(int userId, int assetId);
        Task<PortfolioDetailReportDTO> GetPortfolioDetailAsync(int userId, int portfolioId, int assetId);

        Task<StocksReportDTO> GetStocksAsync(int userId, int assetId);

        Task<CryptoOverviewReportDTO> GetCryptoOverviewAsync(int userId, int assetId, bool includeStables = true);
        Task<CryptoDetailReportDTO> GetCryptoDetailAsync(int userId, int cryptoAssetId, int assetId);

        Task<ContributionsVsPerformanceDTO> GetContributionsVsPerformanceAsync(int userId, int assetId);
    }
}
