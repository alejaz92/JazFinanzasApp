using FluentAssertions;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 21 (Bloque E, Flujo 6 — Viajes): mismo Total que ReportService.GetTripsGeneralStatsAsync/
    // GetTripDetailStatsAsync (ya cubierto por ReportServiceTests), acá se testea lo nuevo del Flujo 6:
    // duración, costo por día, gasto día a día sin huecos y la comparación entre viajes.
    public class TripReportServiceTests
    {
        private const int UserId = 1;
        private static readonly Asset UsdAsset = new() { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" };
        private static readonly DateTime MovementDate = new(2026, 7, 5);

        private readonly Mock<ITripRepository> _tripRepoMock = new();
        private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock = new();
        private readonly Mock<ISharedEventRepository> _sharedEventRepoMock = new();
        private readonly Mock<IAssetRepository> _assetRepoMock = new();
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock = new();
        private readonly TripReportService _sut;

        public TripReportServiceTests()
        {
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(It.IsAny<int>())).ReturnsAsync(new List<SharedEvent>());
            _transactionRepoMock.Setup(r => r.GetTripOwnExpenseTransactionsAsync(It.IsAny<int>())).ReturnsAsync(new List<Transaction>());
            _cardTransactionRepoMock.Setup(r => r.GetTripOwnExpenseCardTransactionsAsync(It.IsAny<int>())).ReturnsAsync(new List<CardTransaction>());
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(UsdAsset);

            _sut = new TripReportService(
                _tripRepoMock.Object,
                _transactionRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _sharedEventRepoMock.Object,
                _assetRepoMock.Object,
                _assetQuoteRepoMock.Object);
        }

        [Fact]
        public async Task GetGeneralAsync_ComputesDurationAndCostPerDay_InclusiveOfBothEndpoints()
        {
            // Bariloche 2026 en el relevamiento (1.6 del plan): 11 días de duración.
            var trip = new Trip
            {
                Id = 5,
                Name = "Bariloche",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 11),
                UserId = UserId
            };
            _tripRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Trip> { trip });

            var sharedEvent = new SharedEvent
            {
                Id = 10,
                Name = "Bariloche 2026",
                Movements = new List<SharedEventMovement>
                {
                    new() { AssetId = 2, Asset = UsdAsset, Date = new DateTime(2026, 1, 3),
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 1100m } } }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = (await _sut.GetGeneralAsync(UserId, 2)).ToList();

            result.Should().ContainSingle();
            result[0].DurationDays.Should().Be(11);
            result[0].Total.Should().Be(1100m);
            result[0].CostPerDay.Should().Be(100m);
        }

        [Fact]
        public async Task GetGeneralAsync_SingleDayTrip_DurationIsOne_NotZero()
        {
            var trip = new Trip { Id = 5, Name = "Día", Type = "DOMESTIC", StartDate = MovementDate, EndDate = MovementDate, UserId = UserId };
            _tripRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Trip> { trip });

            var result = (await _sut.GetGeneralAsync(UserId, 2)).ToList();

            result[0].DurationDays.Should().Be(1);
            result[0].CostPerDay.Should().Be(0m);
        }

        [Fact]
        public async Task GetGeneralAsync_AssetNotFound_ThrowsNotFoundException()
        {
            _assetRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Asset?)null);

            await FluentActions.Invoking(() => _sut.GetGeneralAsync(UserId, 999))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetDetailAsync_TripNotFound_ThrowsNotFoundException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Trip?)null);

            await FluentActions.Invoking(() => _sut.GetDetailAsync(UserId, 99, 2))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetDetailAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Trip { Id = 5, UserId = 999, StartDate = MovementDate, EndDate = MovementDate });

            await FluentActions.Invoking(() => _sut.GetDetailAsync(UserId, 5, 2))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task GetDetailAsync_DailySpending_FillsEveryDayOfTheTrip_ZeroWhereThereIsNoMovement()
        {
            var trip = new Trip { Id = 5, Name = "Bariloche", UserId = UserId, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 3) };
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);
            _tripRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Trip> { trip });

            var sharedEvent = new SharedEvent
            {
                Id = 10,
                Name = "Bariloche 2026",
                Movements = new List<SharedEventMovement>
                {
                    // Solo el día del medio tiene gasto; el primero y el último quedan en 0.
                    new() { AssetId = 2, Asset = UsdAsset, Date = new DateTime(2026, 1, 2),
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 90m } } }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetDetailAsync(UserId, 5, 2);

            result.DailySpending.Should().HaveCount(3);
            result.DailySpending.Should().ContainSingle(d => d.Date == new DateTime(2026, 1, 1) && d.Amount == 0m);
            result.DailySpending.Should().ContainSingle(d => d.Date == new DateTime(2026, 1, 2) && d.Amount == 90m);
            result.DailySpending.Should().ContainSingle(d => d.Date == new DateTime(2026, 1, 3) && d.Amount == 0m);
            result.OutsideTripRangeAmount.Should().Be(0m);
        }

        // Verificado en vivo contra `demo` (Mar Chiquita 2026, producción): dos de los tres movimientos
        // etiquetados al viaje tienen fecha muy anterior a StartDate (pasajes comprados con anticipación,
        // ropa comprada meses antes) -- sin OutsideTripRangeAmount, esa plata desaparecía del gráfico de
        // día a día sin que se note, aunque siguiera contando en Total.
        [Fact]
        public async Task GetDetailAsync_MovementDatedBeforeTripStarts_IsExcludedFromDailySpending_ButCountedInOutsideRangeAmount()
        {
            var trip = new Trip { Id = 5, Name = "Mar Chiquita", UserId = UserId, StartDate = new DateTime(2026, 7, 10), EndDate = new DateTime(2026, 7, 14) };
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);
            _tripRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Trip> { trip });

            var sharedEvent = new SharedEvent
            {
                Id = 10,
                Name = "Mar Chiquita 2026",
                Movements = new List<SharedEventMovement>
                {
                    // Pasaje comprado dos meses antes de que arranque el viaje.
                    new() { AssetId = 2, Asset = UsdAsset, Date = new DateTime(2026, 5, 1),
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 34m } } },
                    // Este sí cae dentro del viaje.
                    new() { AssetId = 2, Asset = UsdAsset, Date = new DateTime(2026, 7, 10),
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 0.13m } } }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetDetailAsync(UserId, 5, 2);

            result.Total.Should().Be(34.13m);
            result.DailySpending.Sum(d => d.Amount).Should().Be(0.13m);
            result.OutsideTripRangeAmount.Should().Be(34m);
            (result.DailySpending.Sum(d => d.Amount) + result.OutsideTripRangeAmount).Should().Be(result.Total);
        }

        [Fact]
        public async Task GetDetailAsync_CostPerDayComparison_IncludesAllTrips_MarkingTheCurrentOne()
        {
            var current = new Trip { Id = 5, Name = "Bariloche", UserId = UserId, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 2) };
            var other = new Trip { Id = 6, Name = "Buenos Aires", UserId = UserId, StartDate = new DateTime(2024, 5, 1), EndDate = new DateTime(2024, 5, 3) };
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(current);
            _tripRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Trip> { current, other });

            var currentEvent = new SharedEvent
            {
                Id = 10,
                Name = "Bariloche 2026",
                Movements = new List<SharedEventMovement>
                {
                    new() { AssetId = 2, Asset = UsdAsset, Date = new DateTime(2026, 1, 1),
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 200m } } }
                }
            };
            var otherEvent = new SharedEvent
            {
                Id = 11,
                Name = "Buenos Aires 2024",
                Movements = new List<SharedEventMovement>
                {
                    new() { AssetId = 2, Asset = UsdAsset, Date = new DateTime(2024, 5, 1),
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 90m } } }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { currentEvent });
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(6)).ReturnsAsync(new List<SharedEvent> { otherEvent });

            var result = await _sut.GetDetailAsync(UserId, 5, 2);

            result.CostPerDay.Should().Be(100m); // 200 / 2 días
            result.CostPerDayComparison.Should().HaveCount(2);
            result.CostPerDayComparison.Should().ContainSingle(c => c.TripId == 5 && c.IsCurrent && c.CostPerDay == 100m);
            result.CostPerDayComparison.Should().ContainSingle(c => c.TripId == 6 && !c.IsCurrent && c.CostPerDay == 30m); // 90 / 3 días
        }
    }
}
