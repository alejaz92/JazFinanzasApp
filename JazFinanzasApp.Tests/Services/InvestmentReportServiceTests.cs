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
        private readonly InvestmentReportService _sut;

        public InvestmentReportServiceTests()
        {
            _sut = new InvestmentReportService(
                _transactionRepoMock.Object,
                _assetRepoMock.Object,
                _assetTypeRepoMock.Object,
                _assetQuoteRepoMock.Object,
                _portfolioRepoMock.Object);
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
            _transactionRepoMock.Setup(r => r.GetPortfolioStatsAsync(UserId, 5)).ReturnsAsync(stats);

            var result = await _sut.GetPortfoliosOverviewAsync(UserId, 5);

            result.Portfolios.Single(p => p.PortfolioId == 1).SharePercent.Should().Be(75m);
            result.Portfolios.Single(p => p.PortfolioId == 2).SharePercent.Should().Be(25m);
        }
    }
}
