using FluentAssertions;
using JazFinanzasApp.API.Business.Services;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 8a, docs/plans/activos/plan-rediseno-reportes.md (T7): el caso real del plan —
    // "Coto", "COTO 3456", "compra coto" → los tres normalizan al mismo texto.
    public class MerchantTextNormalizerTests
    {
        [Theory]
        [InlineData("Coto", "coto")]
        [InlineData("COTO 3456", "coto")]
        [InlineData("compra coto", "coto")]
        [InlineData("Compra en Farmacia Del Sol", "farmacia sol")]
        [InlineData("PAGO TARJETA DEBITO YPF", "ypf")]
        [InlineData("Café Martínez", "cafe martinez")]
        public void Normalize_GroupsVariantsOfTheSameDetail(string detail, string expected)
        {
            MerchantTextNormalizer.Normalize(detail).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1234")]
        [InlineData("compra pago tarjeta")]
        public void Normalize_ReturnsEmpty_WhenDetailHasNoMeaningfulText(string? detail)
        {
            MerchantTextNormalizer.Normalize(detail).Should().BeEmpty();
        }

        [Fact]
        public void Normalize_IsDeterministic()
        {
            var first = MerchantTextNormalizer.Normalize("Compra Supermercado Coto SA");
            var second = MerchantTextNormalizer.Normalize("Compra Supermercado Coto SA");

            first.Should().Be(second);
        }
    }
}
