using FluentAssertions;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 24 (Bloque F, docs/plans/activos/plan-rediseno-reportes-v2.md). La exportación a Excel
    // (planteada originalmente como parte de esta fase) se resolvió del lado del cliente, sin backend
    // — ver la decisión del 2026-09-16 en el plan. Esta fase queda acotada a ReportFavorite.
    public class ReportFavoriteServiceTests
    {
        private readonly Mock<IReportFavoriteRepository> _repoMock;
        private readonly ReportFavoriteService _sut;

        private const int UserId = 1;
        private const int OtherUserId = 2;

        public ReportFavoriteServiceTests()
        {
            _repoMock = new Mock<IReportFavoriteRepository>();
            _sut = new ReportFavoriteService(_repoMock.Object);
        }

        // ── GetAllForUserAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetAllForUserAsync_ReturnsFavoritesOrderedByOrder()
        {
            _repoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<ReportFavorite>
            {
                new() { Id = 2, ReportKey = "cards-general", Order = 1, UserId = UserId },
                new() { Id = 1, ReportKey = "trips-general", Order = 0, UserId = UserId }
            });

            var result = (await _sut.GetAllForUserAsync(UserId)).ToList();

            result.Should().HaveCount(2);
            result[0].ReportKey.Should().Be("trips-general");
            result[1].ReportKey.Should().Be("cards-general");
        }

        // ── CreateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_WithNewReportKey_AppendsAtTheEnd()
        {
            _repoMock.Setup(r => r.GetByReportKeyAsync("trips-general", UserId)).ReturnsAsync((ReportFavorite)null!);
            _repoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<ReportFavorite>
            {
                new() { Id = 1, ReportKey = "cards-general", Order = 3, UserId = UserId }
            });
            _repoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<ReportFavorite>()))
                .ReturnsAsync((ReportFavorite f) => { f.Id = 2; return f; });

            var result = await _sut.CreateAsync(UserId, "trips-general");

            result.ReportKey.Should().Be("trips-general");
            result.Order.Should().Be(4);
            _repoMock.Verify(r => r.AddAsyncReturnObject(It.Is<ReportFavorite>(f =>
                f.ReportKey == "trips-general" && f.Order == 4 && f.UserId == UserId)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AsTheFirstFavorite_StartsAtZero()
        {
            _repoMock.Setup(r => r.GetByReportKeyAsync("trips-general", UserId)).ReturnsAsync((ReportFavorite)null!);
            _repoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<ReportFavorite>());
            _repoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<ReportFavorite>()))
                .ReturnsAsync((ReportFavorite f) => f);

            var result = await _sut.CreateAsync(UserId, "trips-general");

            result.Order.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_WithAlreadyFavoritedReportKey_ThrowsBusinessRuleException()
        {
            _repoMock.Setup(r => r.GetByReportKeyAsync("trips-general", UserId))
                .ReturnsAsync(new ReportFavorite { Id = 1, ReportKey = "trips-general", UserId = UserId });

            var act = () => _sut.CreateAsync(UserId, "trips-general");

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.AddAsyncReturnObject(It.IsAny<ReportFavorite>()), Times.Never);
        }

        // ── DeleteAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_OwnedByAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ReportFavorite { Id = 1, UserId = OtherUserId });

            var act = () => _sut.DeleteAsync(UserId, 1);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ReportFavorite)null!);

            var act = () => _sut.DeleteAsync(UserId, 1);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── ReorderAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task ReorderAsync_WithValidIds_SetsOrderToMatchThePositionGiven()
        {
            var favorites = new List<ReportFavorite>
            {
                new() { Id = 1, ReportKey = "trips-general", Order = 0, UserId = UserId },
                new() { Id = 2, ReportKey = "cards-general", Order = 1, UserId = UserId }
            };
            _repoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(favorites);

            await _sut.ReorderAsync(UserId, new List<int> { 2, 1 });

            _repoMock.Verify(r => r.UpdateAsync(It.Is<ReportFavorite>(f => f.Id == 2 && f.Order == 0)), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.Is<ReportFavorite>(f => f.Id == 1 && f.Order == 1)), Times.Once);
        }

        [Fact]
        public async Task ReorderAsync_MissingOneOfTheCurrentFavorites_ThrowsBusinessRuleException()
        {
            var favorites = new List<ReportFavorite>
            {
                new() { Id = 1, ReportKey = "trips-general", Order = 0, UserId = UserId },
                new() { Id = 2, ReportKey = "cards-general", Order = 1, UserId = UserId }
            };
            _repoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(favorites);

            var act = () => _sut.ReorderAsync(UserId, new List<int> { 1 });

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<ReportFavorite>()), Times.Never);
        }
    }
}
