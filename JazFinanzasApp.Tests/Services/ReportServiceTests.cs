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
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly Mock<ITripRepository> _tripRepoMock;
        private readonly Mock<ISharedEventRepository> _sharedEventRepoMock;
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
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _tripRepoMock = new Mock<ITripRepository>();
            _sharedEventRepoMock = new Mock<ISharedEventRepository>();
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(It.IsAny<int>())).ReturnsAsync(new List<SharedEvent>());

            _sut = new ReportService(
                _transactionRepoMock.Object,
                _assetRepoMock.Object,
                _assetUserRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _assetQuoteRepoMock.Object,
                _assetTypeRepoMock.Object,
                _portfolioRepoMock.Object,
                _tripRepoMock.Object,
                _sharedEventRepoMock.Object);
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

        // ── GetTripsGeneralStatsAsync / GetTripDetailStatsAsync ─────────────────

        private static readonly DateTime MovementDate = new(2026, 7, 5);

        private void SetupUsdAsMainReference()
        {
            _assetUserRepoMock.Setup(r => r.GetMainReferenceAssetAsync(UserId)).ReturnsAsync((Asset_User?)null);
            _assetRepoMock.Setup(r => r.GetAssetByNameAsync("Dolar Estadounidense"))
                .ReturnsAsync(new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" });
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" });
        }

        [Fact]
        public async Task GetTripDetailStatsAsync_GroupsBreakdownByTransactionClass_FromNet()
        {
            SetupUsdAsMainReference();
            var trip = new Trip { Id = 5, Name = "Bariloche", UserId = UserId, StartDate = MovementDate, EndDate = MovementDate };
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);

            var usdAsset = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" };
            var comidaClass = new TransactionClass { Description = "Comida" };
            var sharedEvent = new SharedEvent
            {
                Id = 10,
                Name = "Bariloche 2026",
                Movements = new List<SharedEventMovement>
                {
                    new() { AssetId = 2, Asset = usdAsset, Date = MovementDate, TransactionClass = comidaClass,
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 30m } } },
                    new() { AssetId = 2, Asset = usdAsset, Date = MovementDate, TransactionClass = comidaClass,
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 20m } } }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetTripDetailStatsAsync(UserId, 5);

            result.Total.Should().Be(50m);
            result.Breakdown.Should().ContainSingle(b => b.TransactionClass == "Comida" && b.Amount == 50m);
        }

        [Fact]
        public async Task GetTripDetailStatsAsync_TripNotFound_ThrowsNotFoundException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Trip?)null);

            await FluentActions.Invoking(() => _sut.GetTripDetailStatsAsync(UserId, 99))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetTripDetailStatsAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Trip { Id = 5, UserId = 999 });

            await FluentActions.Invoking(() => _sut.GetTripDetailStatsAsync(UserId, 5))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── Neto según Eventos Compartidos vinculados (plan-viajes-eventos.md, Fase 2) ──

        [Fact]
        public async Task GetTripsGeneralStatsAsync_WithNoLinkedEvents_TotalIsZero()
        {
            SetupUsdAsMainReference();
            var trip = new Trip { Id = 5, Name = "Bariloche", Type = "DOMESTIC", StartDate = MovementDate, EndDate = MovementDate.AddDays(3), UserId = UserId };
            _tripRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Trip> { trip });

            var result = (await _sut.GetTripsGeneralStatsAsync(UserId)).ToList();

            result[0].TotalInReference.Should().Be(0m);
        }

        [Fact]
        public async Task GetTripDetailStatsAsync_SumsNetAcrossLinkedEvents_OpenAndClosed_MultipleCurrencies()
        {
            SetupUsdAsMainReference();
            var trip = new Trip { Id = 5, Name = "Bariloche", UserId = UserId, StartDate = MovementDate, EndDate = MovementDate };
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);

            var usdAsset = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" };
            var arsAsset = new Asset { Id = 3, Name = "Peso Argentino", Symbol = "ARS" };

            var openEvent = new SharedEvent
            {
                Id = 10,
                Name = "Vuelos",
                IsClosed = false,
                Movements = new List<SharedEventMovement>
                {
                    new()
                    {
                        AssetId = 2, Asset = usdAsset, Date = MovementDate,
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 100m } }
                    }
                }
            };

            var closedEvent = new SharedEvent
            {
                Id = 11,
                Name = "Auto",
                IsClosed = true,
                Movements = new List<SharedEventMovement>
                {
                    new()
                    {
                        AssetId = 3, Asset = arsAsset, Date = MovementDate,
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 50000m } }
                    }
                }
            };

            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5))
                .ReturnsAsync(new List<SharedEvent> { openEvent, closedEvent });
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(3, MovementDate, "BLUE")).ReturnsAsync(1000m);

            var result = await _sut.GetTripDetailStatsAsync(UserId, 5);

            result.Total.Should().Be(150m); // 100 USD + (50.000 ARS / 1000 ARS-por-USD)
            result.NetBreakdown.Should().ContainSingle(b => b.EventId == 10 && b.EventName == "Vuelos" && b.Amount == 100m);
            result.NetBreakdown.Should().ContainSingle(b => b.EventId == 11 && b.EventName == "Auto" && b.Amount == 50m);
        }

        [Fact]
        public async Task GetTripDetailStatsAsync_IgnoresSharesOfOtherPeople_OnlyCountsUsersOwnConsumed()
        {
            SetupUsdAsMainReference();
            var trip = new Trip { Id = 5, Name = "Bariloche", UserId = UserId, StartDate = MovementDate, EndDate = MovementDate };
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);

            var usdAsset = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" };
            var sharedEvent = new SharedEvent
            {
                Id = 10,
                Name = "Hospedaje",
                Movements = new List<SharedEventMovement>
                {
                    new()
                    {
                        AssetId = 2, Asset = usdAsset, Date = MovementDate,
                        Shares = new List<SharedEventMovementShare>
                        {
                            new() { PersonId = null, Amount = 20m },
                            new() { PersonId = 8, Amount = 80m }
                        }
                    }
                }
            };

            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5))
                .ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetTripDetailStatsAsync(UserId, 5);

            result.Total.Should().Be(20m);
        }
    }
}
