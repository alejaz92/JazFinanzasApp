using FluentAssertions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 8a, docs/plans/activos/plan-rediseno-reportes.md (T7): variantes del mismo comercio
    // agrupan juntas, un alias manual gana sobre la heurística, un detalle vacío no crea comercio.
    public class MerchantResolverTests
    {
        private readonly Mock<IMerchantRepository> _repoMock;
        private readonly MerchantResolver _sut;

        private const int UserId = 1;

        public MerchantResolverTests()
        {
            _repoMock = new Mock<IMerchantRepository>();
            _sut = new MerchantResolver(_repoMock.Object);
        }

        [Fact]
        public async Task ResolveAsync_WithEmptyDetail_ReturnsNullAndCreatesNothing()
        {
            var result = await _sut.ResolveAsync(UserId, "   ");

            result.Should().BeNull();
            _repoMock.Verify(r => r.FindAliasAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _repoMock.Verify(r => r.CreateMerchantWithAliasAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAsync_WithNullDetail_ReturnsNull()
        {
            var result = await _sut.ResolveAsync(UserId, null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ResolveAsync_FirstTimeSeeingDetail_CreatesMerchantWithAlias()
        {
            _repoMock.Setup(r => r.FindAliasAsync(UserId, "coto")).ReturnsAsync((MerchantAlias?)null);
            _repoMock.Setup(r => r.CreateMerchantWithAliasAsync(UserId, "Coto", "coto"))
                .ReturnsAsync(new Merchant { Id = 42, Name = "Coto", UserId = UserId });

            var result = await _sut.ResolveAsync(UserId, "Coto");

            result.Should().Be(42);
            _repoMock.Verify(r => r.CreateMerchantWithAliasAsync(UserId, "Coto", "coto"), Times.Once);
        }

        [Fact]
        public async Task ResolveAsync_VariantsOfSameDetail_ResolveToSameMerchant()
        {
            var existingAlias = new MerchantAlias { MerchantId = 42, NormalizedDetail = "coto", IsManual = false };
            _repoMock.Setup(r => r.FindAliasAsync(UserId, "coto")).ReturnsAsync(existingAlias);

            var fromShortLabel = await _sut.ResolveAsync(UserId, "Coto");
            var fromCardStatementLabel = await _sut.ResolveAsync(UserId, "COTO 3456");
            var fromManualDetail = await _sut.ResolveAsync(UserId, "compra coto");

            fromShortLabel.Should().Be(42);
            fromCardStatementLabel.Should().Be(42);
            fromManualDetail.Should().Be(42);
            _repoMock.Verify(r => r.CreateMerchantWithAliasAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAsync_WhenAliasIsManual_HonorsTheManualCorrectionInsteadOfCreatingANewMerchant()
        {
            // El usuario ya corrigió "coto" para que apunte al comercio 99 (no al 42 que había
            // creado el resolver originalmente) — una nueva resolución debe respetar eso.
            var manualAlias = new MerchantAlias { MerchantId = 99, NormalizedDetail = "coto", IsManual = true };
            _repoMock.Setup(r => r.FindAliasAsync(UserId, "coto")).ReturnsAsync(manualAlias);

            var result = await _sut.ResolveAsync(UserId, "Coto");

            result.Should().Be(99);
            _repoMock.Verify(r => r.CreateMerchantWithAliasAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAsync_DifferentMerchants_DoNotCollide()
        {
            _repoMock.Setup(r => r.FindAliasAsync(UserId, "coto")).ReturnsAsync((MerchantAlias?)null);
            _repoMock.Setup(r => r.FindAliasAsync(UserId, "farmacia sol")).ReturnsAsync((MerchantAlias?)null);
            _repoMock.Setup(r => r.CreateMerchantWithAliasAsync(UserId, "Coto", "coto"))
                .ReturnsAsync(new Merchant { Id = 1, Name = "Coto", UserId = UserId });
            _repoMock.Setup(r => r.CreateMerchantWithAliasAsync(UserId, "Farmacia del Sol", "farmacia sol"))
                .ReturnsAsync(new Merchant { Id = 2, Name = "Farmacia del Sol", UserId = UserId });

            var coto = await _sut.ResolveAsync(UserId, "Coto");
            var farmacia = await _sut.ResolveAsync(UserId, "Farmacia del Sol");

            coto.Should().Be(1);
            farmacia.Should().Be(2);
        }
    }
}
