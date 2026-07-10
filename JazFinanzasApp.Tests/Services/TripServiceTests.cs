using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Trip;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;
using System.Linq.Expressions;

namespace JazFinanzasApp.Tests.Services
{
    public class TripServiceTests
    {
        private readonly Mock<ITripRepository> _tripRepoMock;
        private readonly TripService _sut;

        private const int UserId = 1;

        public TripServiceTests()
        {
            _tripRepoMock = new Mock<ITripRepository>();
            _sut = new TripService(_tripRepoMock.Object);
        }

        private static Trip BuildTrip(int id = 5, int userId = UserId) => new()
        {
            Id = id,
            Name = "Bariloche 2026",
            Type = "DOMESTIC",
            StartDate = DateTime.UtcNow.Date.AddDays(10),
            EndDate = DateTime.UtcNow.Date.AddDays(20),
            UserId = userId
        };

        private void SetupNoDuplicates()
        {
            _tripRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Trip>());
        }

        // ── GetAllForUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetAllForUserAsync_ReturnsMappedTrips()
        {
            var trips = new List<Trip> { BuildTrip(1), BuildTrip(2) };
            _tripRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(trips);

            var result = (await _sut.GetAllForUserAsync(UserId)).ToList();

            result.Should().HaveCount(2);
            result[0].Name.Should().Be("Bariloche 2026");
            result[0].Type.Should().Be("DOMESTIC");
        }

        // ── Estado derivado ───────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_TripInFuture_StatusIsPlanned()
        {
            var trip = BuildTrip();
            trip.StartDate = DateTime.UtcNow.Date.AddDays(5);
            trip.EndDate = DateTime.UtcNow.Date.AddDays(10);
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Status.Should().Be("PLANNED");
        }

        [Fact]
        public async Task GetByIdAsync_TripOngoing_StatusIsInProgress()
        {
            var trip = BuildTrip();
            trip.StartDate = DateTime.UtcNow.Date.AddDays(-2);
            trip.EndDate = DateTime.UtcNow.Date.AddDays(2);
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Status.Should().Be("IN_PROGRESS");
        }

        [Fact]
        public async Task GetByIdAsync_TripEndsToday_StatusIsInProgress()
        {
            var trip = BuildTrip();
            trip.StartDate = DateTime.UtcNow.Date.AddDays(-5);
            trip.EndDate = DateTime.UtcNow.Date;
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Status.Should().Be("IN_PROGRESS");
        }

        [Fact]
        public async Task GetByIdAsync_TripInPast_StatusIsFinished()
        {
            var trip = BuildTrip();
            trip.StartDate = DateTime.UtcNow.Date.AddDays(-10);
            trip.EndDate = DateTime.UtcNow.Date.AddDays(-5);
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Status.Should().Be("FINISHED");
        }

        // ── GetByIdAsync (validaciones) ───────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_TripNotFound_ThrowsNotFoundException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Trip?)null);

            await FluentActions.Invoking(() => _sut.GetByIdAsync(UserId, 99))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetByIdAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildTrip(5, userId: 2));

            await FluentActions.Invoking(() => _sut.GetByIdAsync(UserId, 5))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── CreateTripAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateTripAsync_ValidTrip_CreatesAndReturnsDTO()
        {
            SetupNoDuplicates();
            var dto = new TripAddDTO
            {
                Name = "Japon 2027",
                Type = "INTERNATIONAL",
                StartDate = new DateTime(2027, 3, 1),
                EndDate = new DateTime(2027, 3, 20)
            };

            Trip? captured = null;
            _tripRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Trip>()))
                .Callback<Trip>(t => captured = t)
                .ReturnsAsync((Trip t) => t);

            var result = await _sut.CreateTripAsync(UserId, dto);

            captured.Should().NotBeNull();
            captured!.UserId.Should().Be(UserId);
            captured.Name.Should().Be("Japon 2027");
            result.Type.Should().Be("INTERNATIONAL");
            result.Status.Should().Be("PLANNED");
        }

        [Fact]
        public async Task CreateTripAsync_EndDateBeforeStartDate_ThrowsBusinessRuleException()
        {
            var dto = new TripAddDTO
            {
                Name = "Viaje",
                Type = "DOMESTIC",
                StartDate = new DateTime(2027, 3, 20),
                EndDate = new DateTime(2027, 3, 1)
            };

            await FluentActions.Invoking(() => _sut.CreateTripAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateTripAsync_InvalidType_ThrowsBusinessRuleException()
        {
            var dto = new TripAddDTO
            {
                Name = "Viaje",
                Type = "OTRO",
                StartDate = new DateTime(2027, 3, 1),
                EndDate = new DateTime(2027, 3, 20)
            };

            await FluentActions.Invoking(() => _sut.CreateTripAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateTripAsync_DuplicateName_ThrowsBusinessRuleException()
        {
            _tripRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
                .ReturnsAsync(new List<Trip> { BuildTrip() });
            var dto = new TripAddDTO
            {
                Name = "Bariloche 2026",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 9, 1),
                EndDate = new DateTime(2026, 9, 10)
            };

            await FluentActions.Invoking(() => _sut.CreateTripAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateTripAsync_SingleDayTrip_IsValid()
        {
            SetupNoDuplicates();
            var date = new DateTime(2027, 3, 1);
            var dto = new TripAddDTO { Name = "Escapada", Type = "DOMESTIC", StartDate = date, EndDate = date };

            _tripRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Trip>()))
                .ReturnsAsync((Trip t) => t);

            var result = await _sut.CreateTripAsync(UserId, dto);

            result.Name.Should().Be("Escapada");
        }

        // ── UpdateTripAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateTripAsync_ValidChanges_UpdatesTrip()
        {
            var trip = BuildTrip();
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(trip);
            SetupNoDuplicates();
            var dto = new TripEditDTO
            {
                Name = "Bariloche invierno 2026",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 15)
            };

            await _sut.UpdateTripAsync(UserId, 5, dto);

            _tripRepoMock.Verify(r => r.UpdateAsync(It.Is<Trip>(t =>
                t.Id == 5 &&
                t.Name == "Bariloche invierno 2026" &&
                t.StartDate == new DateTime(2026, 8, 1) &&
                t.EndDate == new DateTime(2026, 8, 15))), Times.Once);
        }

        [Fact]
        public async Task UpdateTripAsync_TripNotFound_ThrowsNotFoundException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Trip?)null);
            var dto = new TripEditDTO
            {
                Name = "Viaje",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 15)
            };

            await FluentActions.Invoking(() => _sut.UpdateTripAsync(UserId, 99, dto))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateTripAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildTrip(5, userId: 2));
            var dto = new TripEditDTO
            {
                Name = "Viaje",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 15)
            };

            await FluentActions.Invoking(() => _sut.UpdateTripAsync(UserId, 5, dto))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task UpdateTripAsync_EndDateBeforeStartDate_ThrowsBusinessRuleException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildTrip());
            var dto = new TripEditDTO
            {
                Name = "Viaje",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 15),
                EndDate = new DateTime(2026, 8, 1)
            };

            await FluentActions.Invoking(() => _sut.UpdateTripAsync(UserId, 5, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task UpdateTripAsync_DuplicateName_ThrowsBusinessRuleException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildTrip());
            _tripRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
                .ReturnsAsync(new List<Trip> { BuildTrip(6) });
            var dto = new TripEditDTO
            {
                Name = "Bariloche 2026",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 15)
            };

            await FluentActions.Invoking(() => _sut.UpdateTripAsync(UserId, 5, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        // ── DeleteTripAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteTripAsync_ExistingTrip_DeletesIt()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildTrip());

            await _sut.DeleteTripAsync(UserId, 5);

            _tripRepoMock.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task DeleteTripAsync_TripNotFound_ThrowsNotFoundException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Trip?)null);

            await FluentActions.Invoking(() => _sut.DeleteTripAsync(UserId, 99))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteTripAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildTrip(5, userId: 2));

            await FluentActions.Invoking(() => _sut.DeleteTripAsync(UserId, 5))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }
    }
}
