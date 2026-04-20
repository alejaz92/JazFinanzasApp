using FluentAssertions;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class ReportServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly Mock<IAsset_UserRepository> _assetUserRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock;
        private readonly Mock<IAssetTypeRepository> _assetTypeRepoMock;
        private readonly ReportService _sut;

        private const int UserId = 1;

        public ReportServiceTests()
        {
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _assetUserRepoMock = new Mock<IAsset_UserRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _assetQuoteRepoMock = new Mock<IAssetQuoteRepository>();
            _assetTypeRepoMock = new Mock<IAssetTypeRepository>();

            _sut = new ReportService(
                _transactionRepoMock.Object,
                _assetRepoMock.Object,
                _assetUserRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _assetQuoteRepoMock.Object,
                _assetTypeRepoMock.Object);
        }

        // ── GetTotalsBalanceAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetTotalsBalanceAsync_WhenNoReferenceAssets_UsesDollarAsDefault()
        {
            // Arrange
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" };
            var balanceResult = new TotalsBalanceResult { Asset = "Dolar Estadounidense", Symbol = "USD", Color = "#0000ff", Balance = 1500m };

            _assetUserRepoMock.Setup(r => r.GetReferenceAssetsAsync(UserId)).ReturnsAsync(new List<Asset_User>());
            _assetRepoMock.Setup(r => r.GetAssetByNameAsync("Dolar Estadounidense")).ReturnsAsync(dollar);
            _transactionRepoMock.Setup(r => r.GetTotalsBalanceByUserAsync(UserId, dollar)).ReturnsAsync(balanceResult);

            // Act
            var result = await _sut.GetTotalsBalanceAsync(UserId);

            // Assert
            result.Should().HaveCount(1);
            result.First().Asset.Should().Be("Dolar Estadounidense");
            result.First().Balance.Should().Be(1500m);

            _assetRepoMock.Verify(r => r.GetAssetByNameAsync("Dolar Estadounidense"), Times.Once);
        }

        [Fact]
        public async Task GetTotalsBalanceAsync_WhenReferenceAssetsExist_UsesEachReferenceAsset()
        {
            // Arrange
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" };
            var euro = new Asset { Id = 3, Name = "Euro", Symbol = "EUR" };

            var referenceAssets = new List<Asset_User>
            {
                new Asset_User { UserId = UserId, AssetId = 2, Asset = dollar, isReference = true },
                new Asset_User { UserId = UserId, AssetId = 3, Asset = euro, isReference = true }
            };

            _assetUserRepoMock.Setup(r => r.GetReferenceAssetsAsync(UserId)).ReturnsAsync(referenceAssets);
            _transactionRepoMock.Setup(r => r.GetTotalsBalanceByUserAsync(UserId, dollar))
                .ReturnsAsync(new TotalsBalanceResult { Asset = "USD", Symbol = "USD", Balance = 1000m });
            _transactionRepoMock.Setup(r => r.GetTotalsBalanceByUserAsync(UserId, euro))
                .ReturnsAsync(new TotalsBalanceResult { Asset = "EUR", Symbol = "EUR", Balance = 900m });

            // Act
            var result = await _sut.GetTotalsBalanceAsync(UserId);

            // Assert
            result.Should().HaveCount(2);
            _transactionRepoMock.Verify(r => r.GetTotalsBalanceByUserAsync(UserId, It.IsAny<Asset>()), Times.Exactly(2));
        }

        // ── GetBalanceByAssetAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetBalanceByAssetAsync_WhenAssetNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _assetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Asset?)null);

            // Act
            var act = () => _sut.GetBalanceByAssetAsync(UserId, 99);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Asset*");
        }

        [Fact]
        public async Task GetBalanceByAssetAsync_WhenAssetExists_ReturnsMappedBalances()
        {
            // Arrange
            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var balances = new List<BalanceResult>
            {
                new BalanceResult { Account = "Caja", Balance = 5000m },
                new BalanceResult { Account = "Banco", Balance = 10000m }
            };

            _assetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
            _transactionRepoMock.Setup(r => r.GetBalanceByAssetAndUserAsync(1, UserId)).ReturnsAsync(balances);

            // Act
            var result = await _sut.GetBalanceByAssetAsync(UserId, 1);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(b => b.Account == "Caja" && b.Balance == 5000m);
            result.Should().Contain(b => b.Account == "Banco" && b.Balance == 10000m);
        }

        // ── GetIncExpStatsAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetIncExpStatsAsync_WhenAssetIsNotCurrency_ThrowsBusinessRuleException()
        {
            // Arrange
            var stock = new Asset { Id = 5, Name = "YPF", Symbol = "YPFD", AssetTypeId = 2 };
            _assetRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(stock);

            // Act
            var act = () => _sut.GetIncExpStatsAsync(UserId, DateTime.Today, 5);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*moneda*");
        }

        [Fact]
        public async Task GetIncExpStatsAsync_WhenAssetNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _assetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Asset?)null);

            // Act
            var act = () => _sut.GetIncExpStatsAsync(UserId, DateTime.Today, 99);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
