using FluentAssertions;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class QuotePriceResolverTests
    {
        private readonly Mock<IAssetRepository> _assetRepoMock = new();
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock = new();
        private readonly QuotePriceResolver _sut;

        private static readonly DateTime Date = new(2024, 12, 15);

        public QuotePriceResolverTests()
        {
            _sut = new QuotePriceResolver(_assetRepoMock.Object, _assetQuoteRepoMock.Object);
        }

        private void SetupAsset(int id, string symbol) =>
            _assetRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Asset { Id = id, Symbol = symbol });

        [Fact]
        public async Task ResolveAsync_Usd_ReturnsOneWithoutQueryingQuotes()
        {
            SetupAsset(2, "USD");

            var result = await _sut.ResolveAsync(2, Date);

            result.Should().Be(1m);
            _assetQuoteRepoMock.Verify(
                r => r.GetQuotePrice(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAsync_Ars_UsesBlueQuoteForTheDate()
        {
            SetupAsset(1, "ARS");
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(1, Date, "BLUE")).ReturnsAsync(1095m);

            var result = await _sut.ResolveAsync(1, Date);

            result.Should().Be(1095m);
        }

        [Fact]
        public async Task ResolveAsync_OtherAsset_UsesNaQuote()
        {
            SetupAsset(7, "BTC");
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(7, Date, "NA")).ReturnsAsync(95000m);

            var result = await _sut.ResolveAsync(7, Date);

            result.Should().Be(95000m);
        }

        [Fact]
        public async Task ResolveAsync_AssetNotFound_Throws()
        {
            _assetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Asset?)null);

            var act = async () => await _sut.ResolveAsync(99, Date);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
