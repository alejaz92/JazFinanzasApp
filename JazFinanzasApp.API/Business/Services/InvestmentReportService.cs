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
        private readonly IAssetSplitEventRepository _assetSplitEventRepository;

        public InvestmentReportService(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAssetTypeRepository assetTypeRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IPortfolioRepository portfolioRepository,
            IAssetSplitEventRepository assetSplitEventRepository)
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _assetTypeRepository = assetTypeRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _portfolioRepository = portfolioRepository;
            _assetSplitEventRepository = assetSplitEventRepository;
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
        public async Task<PortfoliosOverviewDTO> GetPortfoliosOverviewAsync(int userId, int assetId, bool includeCash = true)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var stats = (await _transactionRepository.GetPortfolioStatsAsync(userId, assetId, includeCash)).ToList();
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
        public async Task<PortfolioDetailReportDTO> GetPortfolioDetailAsync(int userId, int portfolioId, int assetId, bool includeCash = true)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId)
                ?? throw new NotFoundException("Portfolio not found");
            if (portfolio.UserId != userId) throw new UnauthorizedDomainException();

            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            // Mismo criterio que ReportService.GetPortfolioDetailStatsAsync: reusa GetPortfolioStatsAsync
            // para el total en vez de recalcularlo, así no puede divergir de lo que muestra Carteras — General.
            var stats = (await _transactionRepository.GetPortfolioStatsAsync(userId, assetId, includeCash))
                .FirstOrDefault(s => s.PortfolioId == portfolioId);

            var holdings = await _transactionRepository.GetPortfolioHoldingsAsync(userId, portfolioId, assetId, includeCash);
            var valueSeries = await _transactionRepository.GetPortfolioValueByDateAsync(userId, portfolioId, assetId, MonthlySeriesLength, includeCash);

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
                    GainLossPercent = GainLossPercent(h.OriginalValue, h.ActualValue),
                    OriginQuote = h.OriginQuote,
                    CurrentQuote = h.CurrentQuote
                }).ToList(),
                ValueSeries = valueSeries.Select(v => new InvestmentValuePointDTO { Month = v.Date, Value = v.Value }).ToList()
            };
        }

        // Bolsa — General (revisión 2026-09-12, Fase 20a): ganancia/pérdida por ticker + peso dentro
        // de la propia categoría + agregados y evolución por tipo de activo. D-10: cubre todo el
        // entorno BOLSA (los buckets Stocks y Bonds), no solo renta variable como antes — Acción
        // Argentina, CEDEAR, FCI, Acción USA, Bono y Obligación Negociable en un solo reporte.
        // assetTypeId en 0 trae todo (D-11); includeClosed suma las posiciones ya vendidas del todo,
        // apagado por default (D-14).
        public async Task<StocksReportDTO> GetStocksAsync(int userId, int assetId, int assetTypeId = 0, bool includeClosed = false)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var allHoldings = (await _transactionRepository.GetInvestmentHoldingsAsync(userId, assetId))
                .Where(h => h.Bucket == "Stocks" || h.Bucket == "Bonds")
                .ToList();

            // Los agregados por tipo salen del entorno completo, ANTES del filtro (D-11) — así el
            // combo de la barra puede listar todos los tipos con sus totales, se elija el que se elija.
            var types = BuildTypeAggregates(allHoldings);

            var holdings = allHoldings;
            if (assetTypeId != 0)
            {
                var assetType = await _assetTypeRepository.GetByIdAsync(assetTypeId)
                    ?? throw new NotFoundException("Asset type not found");
                holdings = holdings.Where(h => h.AssetTypeName == assetType.Name).ToList();
            }

            var totalOriginal = holdings.Sum(h => h.OriginalValue);
            var totalActual = holdings.Sum(h => h.ActualValue);

            var series = await _transactionRepository.GetStocksValueMonthlySeriesByTypeAsync(userId, referenceAsset, MonthlySeriesLength);

            var closedPositions = includeClosed
                ? (await _transactionRepository.GetClosedInvestmentPositionsAsync(userId, assetId))
                    .Select(c => new ClosedPositionDTO
                    {
                        AssetId = c.AssetId,
                        AssetName = c.AssetName,
                        Symbol = c.Symbol,
                        AssetTypeName = c.AssetTypeName,
                        RealizedResult = c.RealizedResult,
                        LastMovementDate = c.LastMovementDate
                    }).ToList()
                : new List<ClosedPositionDTO>();

            return new StocksReportDTO
            {
                ReferenceAssetSymbol = referenceAsset.Symbol,
                TotalOriginalValue = Math.Round(totalOriginal, 2),
                TotalActualValue = Math.Round(totalActual, 2),
                Types = types,
                Tickers = BuildTickerList(holdings, totalActual),
                ValueSeries = series.Select(p => new StocksMonthlyPointDTO
                {
                    Month = p.Month,
                    ByType = p.ByType.Select(t => new AssetTypeValueDTO { AssetTypeName = t.AssetTypeName, Value = t.Value }).ToList()
                }).ToList(),
                ClosedPositions = closedPositions
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

        // Detalle de un activo (T17, revisión de Bolsa 2026-09-12): línea de cotización (precio, no
        // valor de tenencia) con compras/ventas marcadas encima y el precio promedio de compra como
        // referencia horizontal. Generalizado de "Cryptos — Detalle": el cálculo no tenía nada de
        // cripto adentro, así que Bolsa — Detalle lo reusa entero (D-15/D-17). La ruta de cripto
        // sigue apuntando acá sin cambiar de contrato (T14).
        public async Task<AssetDetailReportDTO> GetAssetDetailAsync(int userId, int assetId, int referenceAssetId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            var referenceAsset = await _assetRepository.GetByIdAsync(referenceAssetId)
                ?? throw new NotFoundException("Asset not found");

            var priceEvolution = (await _assetQuoteRepository.GetAssetEvolutionStats(assetId, MonthlySeriesLength, referenceAssetId)).ToList();
            var balance = await _transactionRepository.GetBalanceByAssetAndUserAsync(assetId, userId);
            var transactions = (await _transactionRepository.GetInvestmentsTransactionsStats(userId, assetId, referenceAssetId)).ToList();
            var splitEvents = (await _assetSplitEventRepository.GetByAssetIdAsync(assetId)).ToList();

            // T16 (D-16): la cotización histórica se ajusta dividiendo por el factor acumulado de los
            // splits POSTERIORES a la fecha de cada punto, sin tope en ningún "asOf" — a diferencia de
            // GetNetWorthMonthlySeriesAsync (que valúa mes a mes y no puede aplicar un split que
            // todavía no pasó a esa altura), acá siempre se ajusta todo el historial a las unidades
            // de HOY, que es lo mismo que ya hace GetInvestmentsTransactionsStats para el precio de
            // cada marca de compra/venta — sin este ajuste las marcas (ya divididas por el factor) no
            // calzan sobre una curva sin dividir.
            decimal SplitFactor(DateTime date) =>
                splitEvents.Where(s => s.Date > date).Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            var adjustedPriceEvolution = priceEvolution
                .Select(p => new { p.Date, Value = p.Value / SplitFactor(p.Date) })
                .ToList();

            // GetAverageBuyValue (nombre heredado de la pantalla vieja) en realidad suma el VALOR neto
            // invertido, no un precio por unidad — compararlo contra PriceEvolution (precio por unidad)
            // daba una línea de referencia sin sentido (ej. "$46" contra una cotización de "$79.000").
            // El precio promedio de compra real es el valor comprado sobre la cantidad comprada, solo
            // compras ("I"), con la cantidad SIN redondear (Transactions redondea a 2 decimales para
            // mostrar, y una cripto con tenencia chica puede redondear a 0).
            var buys = transactions.Where(t => t.MovementType == "I").ToList();
            var buysQuantity = buys.Sum(t => t.Quantity);
            var averageBuyPrice = buysQuantity > 0 ? buys.Sum(t => t.Total) / buysQuantity : 0m;

            var positivePrices = adjustedPriceEvolution.Where(p => p.Value > 0).ToList();

            // D-15: la posición del usuario en este activo, dentro de su propia categoría — Bolsa
            // (Stocks + Bonds, D-10) o Cryptos (CryptoStable + CryptoVolatile), según a cuál pertenezca.
            var allHoldings = (await _transactionRepository.GetInvestmentHoldingsAsync(userId, referenceAssetId)).ToList();
            var holding = allHoldings.FirstOrDefault(h => h.AssetId == assetId);
            AssetPositionDTO position = null;
            if (holding != null)
            {
                static bool IsBolsaBucket(string bucket) => bucket == "Stocks" || bucket == "Bonds";
                var family = IsBolsaBucket(holding.Bucket)
                    ? allHoldings.Where(h => IsBolsaBucket(h.Bucket))
                    : allHoldings.Where(h => h.Bucket == "CryptoStable" || h.Bucket == "CryptoVolatile");
                var totalFamilyActual = family.Sum(h => h.ActualValue);

                position = new AssetPositionDTO
                {
                    Quantity = holding.Quantity,
                    OriginalValue = holding.OriginalValue,
                    ActualValue = holding.ActualValue,
                    GainLossPercent = GainLossPercent(holding.OriginalValue, holding.ActualValue),
                    WeightPercent = totalFamilyActual != 0 ? Math.Round(holding.ActualValue / totalFamilyActual * 100, 2) : 0m
                };
            }

            return new AssetDetailReportDTO
            {
                AssetId = asset.Id,
                AssetName = asset.Name,
                Symbol = asset.Symbol,
                ReferenceAssetSymbol = referenceAsset.Symbol,
                AverageBuyPrice = Math.Round(averageBuyPrice, 2),
                MinPrice = positivePrices.Count > 0 ? Math.Round(positivePrices.Min(p => p.Value), 2) : 0m,
                MaxPrice = adjustedPriceEvolution.Count > 0 ? Math.Round(adjustedPriceEvolution.Max(p => p.Value), 2) : 0m,
                CurrentPrice = adjustedPriceEvolution.Count > 0 ? Math.Round(adjustedPriceEvolution[^1].Value, 2) : 0m,
                PriceEvolution = adjustedPriceEvolution.Select(p => new InvestmentValuePointDTO { Month = p.Date, Value = Math.Round(p.Value, 2) }).ToList(),
                Transactions = transactions.Select(t => new CryptoTransactionMarkerDTO
                {
                    Date = t.Date,
                    Account = t.Account,
                    MovementType = t.MovementType,
                    CommerceType = t.CommerceType,
                    // Cantidad sin redondear (corrección 2026-09-10) — QuotePrice y Total sí, son montos
                    // en la moneda de referencia, no cantidad de cripto.
                    Quantity = t.Quantity,
                    QuotePrice = Math.Round(t.QuotePrice, 2),
                    Total = Math.Round(t.Total, 2)
                }).ToList(),
                BalanceByAccount = balance.Select(b => new AccountHoldingAmountDTO { Account = b.Account, Balance = b.Balance }).ToList(),
                SplitEvents = splitEvents.Select(s => new AssetSplitEventMarkerDTO { Date = s.Date, SplitRatio = s.SplitRatio }).ToList(),
                Position = position
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
                    AssetId = h.AssetId,
                    AssetTypeName = h.AssetTypeName,
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

        // D-12: reemplaza al ranking de 30 barras por ticker — cuatro o cinco barras, una por tipo de
        // activo. Pura — testeable sin mocks, mismo criterio que BuildTickerList.
        public static List<StockTypeAggregateDTO> BuildTypeAggregates(IEnumerable<InvestmentHoldingResult> holdings) =>
            holdings
                .GroupBy(h => h.AssetTypeName)
                .Select(g =>
                {
                    var originalValue = g.Sum(h => h.OriginalValue);
                    var actualValue = g.Sum(h => h.ActualValue);
                    return new StockTypeAggregateDTO
                    {
                        AssetTypeName = g.Key,
                        TickerCount = g.Count(),
                        OriginalValue = Math.Round(originalValue, 2),
                        ActualValue = Math.Round(actualValue, 2),
                        GainLossPercent = GainLossPercent(originalValue, actualValue)
                    };
                })
                .OrderByDescending(t => t.ActualValue)
                .ToList();
    }
}
