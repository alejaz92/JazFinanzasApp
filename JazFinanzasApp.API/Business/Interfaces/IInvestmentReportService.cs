using JazFinanzasApp.API.Business.DTO.InvestmentReport;

namespace JazFinanzasApp.API.Business.Interfaces
{
    // Bloque E, Fase 19 (docs/plans/activos/plan-rediseno-reportes-v2.md): backend de la categoría
    // Inversiones (Flujo 5). assetId es siempre la moneda de referencia elegida en la barra de
    // Reportes (T12) — mismo criterio que INetWorthReportService/ICardReportService.
    public interface IInvestmentReportService
    {
        Task<InvestmentOverviewDTO> GetOverviewAsync(int userId, int assetId);

        // includeCash en false (switch de Carteras — General/Detalle, 2026-09-10) excluye el efectivo
        // (Environment "FIAT") — una cartera lo incluye por diseño (1.3 del plan), pero el switch
        // permite ver solo la parte realmente invertida, con el mismo criterio que ya usa Panorama.
        Task<PortfoliosOverviewDTO> GetPortfoliosOverviewAsync(int userId, int assetId, bool includeCash = true);
        Task<PortfolioDetailReportDTO> GetPortfolioDetailAsync(int userId, int portfolioId, int assetId, bool includeCash = true);

        // Bolsa, revisada (Fase 20a): assetTypeId en 0 trae todo el entorno (D-11); includeClosed
        // suma las posiciones ya vendidas del todo, apagado por default (D-14).
        Task<StocksReportDTO> GetStocksAsync(int userId, int assetId, int assetTypeId = 0, bool includeClosed = false);

        Task<CryptoOverviewReportDTO> GetCryptoOverviewAsync(int userId, int assetId, bool includeStables = true);

        // Detalle de un activo (T17): un solo cálculo para Bolsa y para Cryptos — GetCryptoDetailAsync
        // no tenía nada de cripto adentro. `assetId` es el activo a detallar, `referenceAssetId` la
        // moneda de referencia de la barra de Reportes (mismo orden que ya tenía la firma vieja).
        Task<AssetDetailReportDTO> GetAssetDetailAsync(int userId, int assetId, int referenceAssetId);

        Task<ContributionsVsPerformanceDTO> GetContributionsVsPerformanceAsync(int userId, int assetId);
    }
}
