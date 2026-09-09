using JazFinanzasApp.API.Business.DTO.InvestmentReport;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class InvestmentReportService : IInvestmentReportService
    {
        // Mismo largo que las series mensuales del resto de los reportes (Fases 10, 12, 14).
        private const int MonthlySeriesLength = 12;
        private const string CryptoAssetTypeName = "Criptomoneda";

        // El cálculo de aportes arranca en marzo de 2024 — los 62 movimientos anteriores son ajustes
        // de saldo, no aportes reales (1.5 del plan).
        private static readonly DateTime ContributionsStartMonth = new(2024, 3, 1);

        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetTypeRepository _assetTypeRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IPortfolioRepository _portfolioRepository;

        public InvestmentReportService(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAssetTypeRepository assetTypeRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IPortfolioRepository portfolioRepository)
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _assetTypeRepository = assetTypeRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _portfolioRepository = portfolioRepository;
        }

        // Pura — testeable sin mocks. null cuando OriginalValue es 0 (posición sin costo conocido,
        // ej. un ajuste): un % de ganancia sobre una base $0 no tiene lectura.
        public static decimal? GainLossPercent(decimal originalValue, decimal actualValue) =>
            originalValue != 0 ? Math.Round((actualValue - originalValue) / originalValue * 100, 2) : null;

        // Panorama de inversiones: mapa de bloques de todo (bolsa, cripto, bonos) coloreado por
        // ganancia/pérdida + línea del valor total con los aportes marcados como puntos.
        public async Task<InvestmentOverviewDTO> GetOverviewAsync(int userId, int assetId)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var holdings = (await _transactionRepository.GetInvestmentHoldingsAsync(userId, assetId)).ToList();
            var totalOriginal = holdings.Sum(h => h.OriginalValue);
            var totalActual = holdings.Sum(h => h.ActualValue);

            var series = await _transactionRepository.GetNetWorthMonthlySeriesAsync(userId, referenceAsset, MonthlySeriesLength);
            var valueSeries = series
                .Select(p => new InvestmentValuePointDTO { Month = p.Month, Value = Math.Round(p.Stocks + p.CryptoStable + p.CryptoVolatile + p.Bonds, 2) })
                .ToList();

            var since = valueSeries.Count > 0 ? valueSeries[0].Month : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var contributionsByMonth = await _transactionRepository.GetInvestmentContributionsByMonthAsync(userId, assetId, since);

            return new InvestmentOverviewDTO
            {
                ReferenceAssetSymbol = referenceAsset.Symbol,
                TotalOriginalValue = Math.Round(totalOriginal, 2),
                TotalActualValue = Math.Round(totalActual, 2),
                GainLossPercent = GainLossPercent(totalOriginal, totalActual),
                Holdings = holdings.Select(h => new InvestmentHoldingDTO
                {
                    AssetId = h.AssetId,
                    AssetName = h.AssetName,
                    Symbol = h.Symbol,
                    Bucket = h.Bucket,
                    Quantity = h.Quantity,
                    OriginalValue = h.OriginalValue,
                    ActualValue = h.ActualValue,
                    GainLossPercent = GainLossPercent(h.OriginalValue, h.ActualValue)
                }).ToList(),
                ValueSeries = valueSeries,
                ContributionMarkers = contributionsByMonth
                    .Select(c => new InvestmentContributionMarkerDTO { Month = c.Month, Contributed = c.Contributed, Withdrawn = c.Withdrawn })
                    .ToList()
            };
        }

        // Carteras — General: barras enfrentadas invertido vs valor actual (reusa GetPortfolioStatsAsync,
        // docs/plans/completados/portfolios-estadisticas.md) + distribución (SharePercent).
        public async Task<PortfoliosOverviewDTO> GetPortfoliosOverviewAsync(int userId, int assetId)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var stats = (await _transactionRepository.GetPortfolioStatsAsync(userId, assetId)).ToList();
            var totalActual = stats.Sum(s => s.ActualValue);

            return new PortfoliosOverviewDTO
            {
                ReferenceAssetSymbol = referenceAsset.Symbol,
                Portfolios = stats.Select(s => new PortfolioOverviewItemDTO
                {
                    PortfolioId = s.PortfolioId,
                    PortfolioName = s.PortfolioName,
                    IsDefault = s.IsDefault,
                    OriginalValue = s.OriginalValue,
                    ActualValue = s.ActualValue,
                    GainLossPercent = GainLossPercent(s.OriginalValue, s.ActualValue),
                    SharePercent = totalActual != 0 ? Math.Round(s.ActualValue / totalActual * 100, 2) : 0m
                }).ToList()
            };
        }

        // Carteras — Detalle: composición (Holdings, con drill-down por cuenta) + evolución de valor.
        // Es el reporte que hoy no existe y por eso las carteras están vacías (1.5 del plan) — los
        // datos ya estaban en ReportService desde portfolios-estadisticas.md, acá solo se exponen con
        // la moneda de referencia elegida en la barra de Reportes en vez de la resuelta por Asset_User.
        public async Task<PortfolioDetailReportDTO> GetPortfolioDetailAsync(int userId, int portfolioId, int assetId)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId)
                ?? throw new NotFoundException("Portfolio not found");
            if (portfolio.UserId != userId) throw new UnauthorizedDomainException();

            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            // Mismo criterio que ReportService.GetPortfolioDetailStatsAsync: reusa GetPortfolioStatsAsync
            // para el total en vez de recalcularlo, así no puede divergir de lo que muestra Carteras — General.
            var stats = (await _transactionRepository.GetPortfolioStatsAsync(userId, assetId))
                .FirstOrDefault(s => s.PortfolioId == portfolioId);

            var holdings = await _transactionRepository.GetPortfolioHoldingsAsync(userId, portfolioId, assetId);
            var valueSeries = await _transactionRepository.GetPortfolioValueByDateAsync(userId, portfolioId, assetId, MonthlySeriesLength);

            return new PortfolioDetailReportDTO
            {
                PortfolioId = portfolioId,
                PortfolioName = portfolio.Name,
                ReferenceAssetSymbol = referenceAsset.Symbol,
                OriginalValue = stats?.OriginalValue ?? 0m,
                ActualValue = stats?.ActualValue ?? 0m,
                GainLossPercent = GainLossPercent(stats?.OriginalValue ?? 0m, stats?.ActualValue ?? 0m),
                Holdings = holdings.Select(h => new PortfolioHoldingItemDTO
                {
                    AssetType = h.AssetType,
                    AssetName = h.AssetName,
                    Symbol = h.Symbol,
                    AccountName = h.AccountName,
                    Quantity = h.Quantity,
                    OriginalValue = h.OriginalValue,
                    ActualValue = h.ActualValue,
                    GainLossPercent = GainLossPercent(h.OriginalValue, h.ActualValue)
                }).ToList(),
                ValueSeries = valueSeries.Select(v => new InvestmentValuePointDTO { Month = v.Date, Value = v.Value }).ToList()
            };
        }

        // Bolsa: ganancia/pérdida por ticker + peso dentro de la propia categoría, sin acotar a un
        // solo AssetType (a diferencia de la pantalla vieja) — junta Acción Argentina, CEDEAR, FCI y
        // Acción USA, que es como el Flujo 5 describe "Bolsa" (un solo reporte, no uno por tipo).
        public async Task<StocksReportDTO> GetStocksAsync(int userId, int assetId)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var holdings = (await _transactionRepository.GetInvestmentHoldingsAsync(userId, assetId))
                .Where(h => h.Bucket == "Stocks")
                .ToList();

            var totalOriginal = holdings.Sum(h => h.OriginalValue);
            var totalActual = holdings.Sum(h => h.ActualValue);

            return new StocksReportDTO
            {
                ReferenceAssetSymbol = referenceAsset.Symbol,
                TotalOriginalValue = Math.Round(totalOriginal, 2),
                TotalActualValue = Math.Round(totalActual, 2),
                Tickers = BuildTickerList(holdings, totalActual)
            };
        }

        // Cryptos — General: evolución del valor + compras por mes + distribución. includeStables
        // filtra DAI/USDT/USDC del listado y de los totales — mismo toggle que ya existía en la
        // pantalla vieja (ReportService.GetCryptoGralStatsAsync), sin volver a pedirle la lista a la
        // base: alcanza con no contar el bucket CryptoStable.
        public async Task<CryptoOverviewReportDTO> GetCryptoOverviewAsync(int userId, int assetId, bool includeStables = true)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            var cryptoType = await _assetTypeRepository.GetByName(CryptoAssetTypeName);

            var holdings = (await _transactionRepository.GetInvestmentHoldingsAsync(userId, assetId))
                .Where(h => h.Bucket == "CryptoVolatile" || (includeStables && h.Bucket == "CryptoStable"))
                .ToList();

            var totalOriginal = holdings.Sum(h => h.OriginalValue);
            var totalActual = holdings.Sum(h => h.ActualValue);

            var valueEvolution = await _transactionRepository.GetCryptoStatsByDateAsync(userId, cryptoType.Id, cryptoType.Environment, 0, includeStables, assetId);
            var purchasesByMonth = await _transactionRepository.GetInvestmentsHoldingsStats(userId, cryptoType.Id, cryptoType.Environment, 0, includeStables, MonthlySeriesLength, assetId);

            return new CryptoOverviewReportDTO
            {
                ReferenceAssetSymbol = referenceAsset.Symbol,
                TotalOriginalValue = Math.Round(totalOriginal, 2),
                TotalActualValue = Math.Round(totalActual, 2),
                Holdings = BuildTickerList(holdings, totalActual),
                ValueEvolution = valueEvolution.Select(v => new InvestmentValuePointDTO { Month = v.Date, Value = Math.Round(v.Value, 2) }).ToList(),
                PurchasesByMonth = purchasesByMonth.Select(p => new CryptoPurchaseMonthDTO { Date = p.Date, CommerceType = p.CommerceType, Value = Math.Round(p.Value, 2) }).ToList()
            };
        }

        // Cryptos — Detalle: línea de cotización (precio, no valor de tenencia) con compras/ventas
        // marcadas encima y el precio promedio de compra como referencia horizontal.
        public async Task<CryptoDetailReportDTO> GetCryptoDetailAsync(int userId, int cryptoAssetId, int assetId)
        {
            var asset = await _assetRepository.GetByIdAsync(cryptoAssetId)
                ?? throw new NotFoundException("Asset not found");
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var priceEvolution = (await _assetQuoteRepository.GetAssetEvolutionStats(cryptoAssetId, MonthlySeriesLength, assetId)).ToList();
            var balance = await _transactionRepository.GetBalanceByAssetAndUserAsync(cryptoAssetId, userId);
            var transactions = (await _transactionRepository.GetInvestmentsTransactionsStats(userId, cryptoAssetId, assetId)).ToList();

            // GetAverageBuyValue (nombre heredado de la pantalla vieja) en realidad suma el VALOR neto
            // invertido, no un precio por unidad — compararlo contra PriceEvolution (precio por unidad)
            // daba una línea de referencia sin sentido (ej. "$46" contra una cotización de "$79.000").
            // El precio promedio de compra real es el valor comprado sobre la cantidad comprada, solo
            // compras ("I"), con la cantidad SIN redondear (Transactions redondea a 2 decimales para
            // mostrar, y una cripto con tenencia chica puede redondear a 0).
            var buys = transactions.Where(t => t.MovementType == "I").ToList();
            var buysQuantity = buys.Sum(t => t.Quantity);
            var averageBuyPrice = buysQuantity > 0 ? buys.Sum(t => t.Total) / buysQuantity : 0m;

            var positivePrices = priceEvolution.Where(p => p.Value > 0).ToList();

            return new CryptoDetailReportDTO
            {
                AssetId = asset.Id,
                AssetName = asset.Name,
                Symbol = asset.Symbol,
                ReferenceAssetSymbol = referenceAsset.Symbol,
                AverageBuyPrice = Math.Round(averageBuyPrice, 2),
                MinPrice = positivePrices.Count > 0 ? Math.Round(positivePrices.Min(p => p.Value), 2) : 0m,
                MaxPrice = priceEvolution.Count > 0 ? Math.Round(priceEvolution.Max(p => p.Value), 2) : 0m,
                CurrentPrice = priceEvolution.Count > 0 ? Math.Round(priceEvolution[^1].Value, 2) : 0m,
                PriceEvolution = priceEvolution.Select(p => new InvestmentValuePointDTO { Month = p.Date, Value = Math.Round(p.Value, 2) }).ToList(),
                Transactions = transactions.Select(t => new CryptoTransactionMarkerDTO
                {
                    Date = t.Date,
                    Account = t.Account,
                    MovementType = t.MovementType,
                    CommerceType = t.CommerceType,
                    Quantity = Math.Round(t.Quantity, 2),
                    QuotePrice = Math.Round(t.QuotePrice, 2),
                    Total = Math.Round(t.Total, 2)
                }).ToList(),
                BalanceByAccount = balance.Select(b => new AccountHoldingAmountDTO { Account = b.Account, Balance = Math.Round(b.Balance, 2) }).ToList()
            };
        }

        // Aportes vs rendimiento: cascada valor al inicio → aportes → retiros → valorización → valor
        // al final, arrancando en marzo de 2024 (1.5). InitialValue se mide a fin de febrero de 2024
        // (el mes ANTERIOR al primero que cuenta como aporte real) reusando GetNetWorthMonthlySeriesAsync
        // — el mismo cálculo que ya sostiene la línea de Patrimonio (Fases 10/16/18), así que hereda
        // sus correcciones (ej. el tope de splits del 2026-09-08) sin duplicar lógica.
        public async Task<ContributionsVsPerformanceDTO> GetContributionsVsPerformanceAsync(int userId, int assetId)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var initialSnapshotMonth = ContributionsStartMonth.AddMonths(-1);
            var today = DateTime.Today;
            var monthsSpan = ((today.Year - initialSnapshotMonth.Year) * 12 + today.Month - initialSnapshotMonth.Month) + 1;

            var series = (await _transactionRepository.GetNetWorthMonthlySeriesAsync(userId, referenceAsset, monthsSpan)).ToList();
            decimal InvestedValue(NetWorthMonthlyPointResult p) => p.Stocks + p.CryptoStable + p.CryptoVolatile + p.Bonds;

            var initialValue = series.Count > 0 ? InvestedValue(series[0]) : 0m;
            var finalValue = series.Count > 0 ? InvestedValue(series[^1]) : 0m;

            var monthly = await _transactionRepository.GetInvestmentContributionsByMonthAsync(userId, assetId, ContributionsStartMonth);
            var contributed = monthly.Sum(m => m.Contributed);
            var withdrawn = monthly.Sum(m => m.Withdrawn);
            var valuation = finalValue - initialValue - contributed + withdrawn;

            return new ContributionsVsPerformanceDTO
            {
                ReferenceAssetSymbol = referenceAsset.Symbol,
                StartMonth = ContributionsStartMonth,
                InitialValue = Math.Round(initialValue, 2),
                Contributed = Math.Round(contributed, 2),
                Withdrawn = Math.Round(withdrawn, 2),
                Valuation = Math.Round(valuation, 2),
                FinalValue = Math.Round(finalValue, 2)
            };
        }

        // Pura — testeable sin mocks. Comparte Bolsa (WeightPercent dentro de Bolsa) y Cryptos —
        // General (WeightPercent dentro de Cryptos): en los dos casos el peso es relativo al total
        // de la propia categoría, no al patrimonio entero.
        public static List<StockTickerReportDTO> BuildTickerList(IEnumerable<InvestmentHoldingResult> holdings, decimal totalActual) =>
            holdings
                .Select(h => new StockTickerReportDTO
                {
                    AssetName = h.AssetName,
                    Symbol = h.Symbol,
                    Quantity = h.Quantity,
                    OriginalValue = h.OriginalValue,
                    ActualValue = h.ActualValue,
                    GainLossAmount = Math.Round(h.ActualValue - h.OriginalValue, 2),
                    GainLossPercent = GainLossPercent(h.OriginalValue, h.ActualValue),
                    WeightPercent = totalActual != 0 ? Math.Round(h.ActualValue / totalActual * 100, 2) : 0m
                })
                .OrderByDescending(t => t.ActualValue)
                .ToList();
    }
}
