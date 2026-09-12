using FluentAssertions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 19 (Inversiones): la mayor parte del servicio compone repositorios que ya tienen sus
    // propios tests (GetPortfolioStatsAsync, GetInvestmentHoldingsAsync, GetNetWorthMonthlySeriesAsync,
    // etc. en TransactionRepositoryTests) — acá se cubre la lógica propia del servicio: el % de
    // ganancia/pérdida y la cascada de Aportes vs rendimiento.
    public class InvestmentReportServiceTests
    {
        private const int UserId = 1;

        private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
        private readonly Mock<IAssetRepository> _assetRepoMock = new();
        private readonly Mock<IAssetTypeRepository> _assetTypeRepoMock = new();
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock = new();
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock = new();
        private readonly Mock<IAssetSplitEventRepository> _assetSplitEventRepoMock = new();
        private readonly InvestmentReportService _sut;

        public InvestmentReportServiceTests()
        {
            _sut = new InvestmentReportService(
                _transactionRepoMock.Object,
                _assetRepoMock.Object,
                _assetTypeRepoMock.Object,
                _assetQuoteRepoMock.Object,
                _portfolioRepoMock.Object,
                _assetSplitEventRepoMock.Object);

            // Defaults vacíos — la mayoría de los tests no necesita splits ni tenencias; los que sí
            // (T16, D-15) pisan estos setups con su propio escenario.
            _assetSplitEventRepoMock.Setup(r => r.GetByAssetIdAsync(It.IsAny<int>())).ReturnsAsync(new List<AssetSplitEvent>());
            _transactionRepoMock.Setup(r => r.GetInvestmentHoldingsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<InvestmentHoldingResult>());
        }

        [Fact]
        public void GainLossPercent_WithOriginalValue_ComputesPercent()
        {
            InvestmentReportService.GainLossPercent(1000m, 1500m).Should().Be(50m);
            InvestmentReportService.GainLossPercent(1000m, 800m).Should().Be(-20m);
        }

        [Fact]
        public void GainLossPercent_WithZeroOriginalValue_ReturnsNull()
        {
            InvestmentReportService.GainLossPercent(0m, 500m).Should().BeNull();
        }

        [Fact]
        public async Task GetContributionsVsPerformanceAsync_ComputesValuationAsThePlugBetweenSnapshotsAndFlows()
        {
            var referenceAsset = new Asset { Id = 5, Symbol = "USD", Name = "Dolar Estadounidense" };
            _assetRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(referenceAsset);

            // Snapshot de Patrimonio (GetNetWorthMonthlySeriesAsync mockeado entero — su propia
            // lógica ya la cubre TransactionRepositoryTests): el primer punto es "antes de marzo de
            // 2024" (InitialValue), el último es hoy (FinalValue). El período/cantidad de meses
            // pedido no importa para este test, solo lo que devuelve.
            var series = new List<NetWorthMonthlyPointResult>
            {
                new() { Month = new DateTime(2024, 2, 1), Stocks = 1000m, CryptoStable = 0m, CryptoVolatile = 0m, Bonds = 0m },
                new() { Month = new DateTime(2026, 9, 1), Stocks = 1400m, CryptoStable = 100m, CryptoVolatile = 0m, Bonds = 0m }
            };
            _transactionRepoMock
                .Setup(r => r.GetNetWorthMonthlySeriesAsync(UserId, referenceAsset, It.IsAny<int>()))
                .ReturnsAsync(series);

            var monthly = new List<InvestmentContributionMonthResult>
            {
                new() { Month = new DateTime(2024, 3, 1), Contributed = 500m, Withdrawn = 0m },
                new() { Month = new DateTime(2024, 4, 1), Contributed = 0m, Withdrawn = 100m }
            };
            _transactionRepoMock
                .Setup(r => r.GetInvestmentContributionsByMonthAsync(UserId, 5, new DateTime(2024, 3, 1)))
                .ReturnsAsync(monthly);

            var result = await _sut.GetContributionsVsPerformanceAsync(UserId, 5);

            result.InitialValue.Should().Be(1000m);
            result.FinalValue.Should().Be(1500m); // 1400 + 100
            result.Contributed.Should().Be(500m);
            result.Withdrawn.Should().Be(100m);
            // Valorización: lo que no explican ni los aportes ni los retiros.
            result.Valuation.Should().Be(1500m - 1000m - 500m + 100m); // 100m
        }

        // Fase 20, revisión visual con `demo`: el precio promedio de compra se graficaba como una
        // línea horizontal a "$46" contra una cotización de "$79.000" — GetAverageBuyValue (nombre
        // heredado de la pantalla vieja) suma el VALOR neto invertido, no un precio por unidad.
        // Renombrado a GetAssetDetailAsync (T17, revisión de Bolsa 2026-09-12): el cálculo no tenía
        // nada de cripto adentro, la ruta de cripto sigue apuntando acá sin cambiar de contrato.
        [Fact]
        public async Task GetAssetDetailAsync_ComputesAverageBuyPriceAsWeightedAverageOfBuys_NotRawInvestedValue()
        {
            var crypto = new Asset { Id = 4, Symbol = "BTC", Name = "Bitcoin" };
            var referenceAsset = new Asset { Id = 2, Symbol = "USD" };
            _assetRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(crypto);
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(referenceAsset);

            _assetQuoteRepoMock
                .Setup(r => r.GetAssetEvolutionStats(4, It.IsAny<int>(), 2))
                .ReturnsAsync(new List<CryptoStatsByDateResult> { new() { Date = new DateTime(2026, 9, 9), Value = 79009.54m } });
            _transactionRepoMock.Setup(r => r.GetBalanceByAssetAndUserAsync(4, UserId)).ReturnsAsync(new List<BalanceResult>());

            // Una compra chica (mismo escenario que `demo`): 46.49 USD invertidos en ~0.00049 BTC —
            // Quantity real, sin redondear (a 2 decimales redondearía a 0 y rompería la división).
            var buyQuantity = 0.00048936m;
            var transactions = new List<InvestmentTransactionsResult>
            {
                new() { Date = new DateTime(2024, 12, 2), Account = "Exchange", MovementType = "I", CommerceType = "Fiat/Crypto Commerce", Quantity = buyQuantity, QuotePrice = 95000.14m, Total = 46.49m }
            };
            _transactionRepoMock.Setup(r => r.GetInvestmentsTransactionsStats(UserId, 4, 2)).ReturnsAsync(transactions);

            var result = await _sut.GetAssetDetailAsync(UserId, 4, 2);

            result.AverageBuyPrice.Should().Be(Math.Round(46.49m / buyQuantity, 2));
            result.AverageBuyPrice.Should().BeGreaterThan(50000m); // no el bug viejo: ~46 (el total invertido, no un precio)
        }

        // T16/D-16 (revisión de Bolsa, 2026-09-12): la cotización histórica se ajusta dividiendo por
        // el factor acumulado de los splits POSTERIORES a la fecha de cada punto — mismo criterio que
        // ya usa GetInvestmentsTransactionsStats para el precio de las marcas de compra/venta, para
        // que las dos series queden en las mismas unidades ("de hoy") y calcen sobre el gráfico.
        [Fact]
        public async Task GetAssetDetailAsync_AdjustsHistoricalPriceForSplitsAfterQuoteDate()
        {
            var asset = new Asset { Id = 7, Symbol = "SPY", Name = "SPDR S&P 500" };
            var referenceAsset = new Asset { Id = 2, Symbol = "USD" };
            _assetRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(asset);
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(referenceAsset);

            var splitDate = new DateTime(2026, 5, 29);
            _assetSplitEventRepoMock.Setup(r => r.GetByAssetIdAsync(7))
                .ReturnsAsync(new List<AssetSplitEvent> { new() { AssetId = 7, Date = splitDate, SplitRatio = 3m } });

            // Cotización real, sin ajustar: $600 antes del split (3:1), $200 después — el mismo valor
            // económico, expresado en unidades distintas. Ajustada, la serie queda plana en $200.
            _assetQuoteRepoMock.Setup(r => r.GetAssetEvolutionStats(7, It.IsAny<int>(), 2)).ReturnsAsync(new List<CryptoStatsByDateResult>
            {
                new() { Date = splitDate.AddMonths(-1), Value = 600m },
                new() { Date = splitDate.AddMonths(1), Value = 200m }
            });
            _transactionRepoMock.Setup(r => r.GetBalanceByAssetAndUserAsync(7, UserId)).ReturnsAsync(new List<BalanceResult>());
            _transactionRepoMock.Setup(r => r.GetInvestmentsTransactionsStats(UserId, 7, 2)).ReturnsAsync(new List<InvestmentTransactionsResult>());

            var result = await _sut.GetAssetDetailAsync(UserId, 7, 2);

            result.PriceEvolution.Single(p => p.Month == splitDate.AddMonths(-1)).Value.Should().Be(200m); // 600 / 3, no 600
            result.PriceEvolution.Single(p => p.Month == splitDate.AddMonths(1)).Value.Should().Be(200m);
            result.SplitEvents.Should().ContainSingle(s => s.Date == splitDate && s.SplitRatio == 3m);
        }

        [Fact]
        public async Task GetPortfoliosOverviewAsync_ComputesSharePercentOfTotalActualValue()
        {
            var referenceAsset = new Asset { Id = 5, Symbol = "USD" };
            _assetRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(referenceAsset);

            var stats = new List<PortfolioStatsResult>
            {
                new() { PortfolioId = 1, PortfolioName = "Default", IsDefault = true, OriginalValue = 1000m, ActualValue = 3000m },
                new() { PortfolioId = 2, PortfolioName = "Cripto", IsDefault = false, OriginalValue = 500m, ActualValue = 1000m }
            };
            _transactionRepoMock.Setup(r => r.GetPortfolioStatsAsync(UserId, 5, true)).ReturnsAsync(stats);

            var result = await _sut.GetPortfoliosOverviewAsync(UserId, 5);

            result.Portfolios.Single(p => p.PortfolioId == 1).SharePercent.Should().Be(75m);
            result.Portfolios.Single(p => p.PortfolioId == 2).SharePercent.Should().Be(25m);
        }

        // D-10 (revisión de Bolsa, 2026-09-12): Bolsa pasa a cubrir todo el entorno, no solo el
        // bucket "Stocks" — los bonos, hoy invisibles en cualquier reporte, entran al total.
        [Fact]
        public async Task GetStocksAsync_IncludesBondsBucketInTotal()
        {
            var referenceAsset = new Asset { Id = 2, Symbol = "USD" };
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(referenceAsset);

            var holdings = new List<InvestmentHoldingResult>
            {
                new() { AssetId = 1, AssetName = "Galicia", Symbol = "GGAL", AssetTypeName = "Accion Argentina", Bucket = "Stocks", Quantity = 10m, OriginalValue = 200m, ActualValue = 300m },
                new() { AssetId = 2, AssetName = "Bono AL30", Symbol = "AL30", AssetTypeName = "Bono", Bucket = "Bonds", Quantity = 5m, OriginalValue = 300m, ActualValue = 375m },
                new() { AssetId = 3, AssetName = "Bitcoin", Symbol = "BTC", AssetTypeName = "Criptomoneda", Bucket = "CryptoVolatile", Quantity = 1m, OriginalValue = 1000m, ActualValue = 1200m }
            };
            _transactionRepoMock.Setup(r => r.GetInvestmentHoldingsAsync(UserId, 2)).ReturnsAsync(holdings);
            _transactionRepoMock.Setup(r => r.GetStocksValueMonthlySeriesByTypeAsync(UserId, referenceAsset, It.IsAny<int>()))
                .ReturnsAsync(new List<StocksMonthlyPointResult>());

            var result = await _sut.GetStocksAsync(UserId, 2);

            result.Tickers.Should().HaveCount(2); // GGAL + AL30, no BTC
            result.Tickers.Should().Contain(t => t.Symbol == "AL30");
            result.TotalActualValue.Should().Be(675m); // 300 + 375, sin el bucket cripto
            result.Types.Should().Contain(t => t.AssetTypeName == "Bono" && t.ActualValue == 375m);
        }

        // D-11: el filtro de tipo de activo recorta Tickers y los totales, pero no Types — el combo
        // de la barra sigue viendo todos los tipos con sus agregados, se elija el que se elija.
        [Fact]
        public async Task GetStocksAsync_FiltersTickersByAssetType_ButKeepsAllTypesInAggregate()
        {
            var referenceAsset = new Asset { Id = 2, Symbol = "USD" };
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(referenceAsset);
            _assetTypeRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(new AssetType { Id = 4, Name = "CEDEAR" });

            var holdings = new List<InvestmentHoldingResult>
            {
                new() { AssetId = 1, AssetName = "Galicia", Symbol = "GGAL", AssetTypeName = "Accion Argentina", Bucket = "Stocks", Quantity = 10m, OriginalValue = 200m, ActualValue = 300m },
                new() { AssetId = 2, AssetName = "Apple", Symbol = "AAPL", AssetTypeName = "CEDEAR", Bucket = "Stocks", Quantity = 3m, OriginalValue = 500m, ActualValue = 600m }
            };
            _transactionRepoMock.Setup(r => r.GetInvestmentHoldingsAsync(UserId, 2)).ReturnsAsync(holdings);
            _transactionRepoMock.Setup(r => r.GetStocksValueMonthlySeriesByTypeAsync(UserId, referenceAsset, It.IsAny<int>()))
                .ReturnsAsync(new List<StocksMonthlyPointResult>());

            var result = await _sut.GetStocksAsync(UserId, 2, assetTypeId: 4);

            result.Tickers.Should().ContainSingle(t => t.Symbol == "AAPL");
            result.TotalActualValue.Should().Be(600m);
            result.Types.Should().HaveCount(2); // Accion Argentina y CEDEAR, pese al filtro
        }

        // D-14: apagado por default — el reporte no trae posiciones cerradas si no se piden.
        [Fact]
        public async Task GetStocksAsync_WithIncludeClosedFalse_DoesNotQueryClosedPositions()
        {
            var referenceAsset = new Asset { Id = 2, Symbol = "USD" };
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(referenceAsset);
            _transactionRepoMock.Setup(r => r.GetInvestmentHoldingsAsync(UserId, 2)).ReturnsAsync(new List<InvestmentHoldingResult>());
            _transactionRepoMock.Setup(r => r.GetStocksValueMonthlySeriesByTypeAsync(UserId, referenceAsset, It.IsAny<int>()))
                .ReturnsAsync(new List<StocksMonthlyPointResult>());

            var result = await _sut.GetStocksAsync(UserId, 2);

            result.ClosedPositions.Should().BeEmpty();
            _transactionRepoMock.Verify(r => r.GetClosedInvestmentPositionsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }
}
