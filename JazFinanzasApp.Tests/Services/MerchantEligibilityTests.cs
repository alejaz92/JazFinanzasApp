using FluentAssertions;
using JazFinanzasApp.API.Business.Services;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 10, docs/plans/activos/plan-rediseno-reportes.md: los detalles que escribe la propia
    // app nunca son un comercio. Sobre el historial real, sin este filtro el comercio más grande
    // del usuario era "intercambio" — 236 transferencias entre sus propias cuentas.
    public class MerchantEligibilityTests
    {
        [Theory]
        [InlineData("Intercambio")]
        [InlineData("intercambio")]
        [InlineData("INTERCAMBIO")]
        [InlineData("General")]
        [InlineData("BalanceAdj")]
        [InlineData("Refund")]
        [InlineData("Evento compartido")]
        [InlineData("Deposito")]
        [InlineData("Depósito")]
        [InlineData("Reintegro en otra Cuenta")]
        [InlineData("Currency Exchange")]
        [InlineData("Trading")]
        [InlineData("Recuento")]
        [InlineData("Ajuste")]
        public void IsSystemDetail_ForAppGeneratedDetail_ReturnsTrue(string detail)
        {
            MerchantEligibility.IsSystemDetail(detail).Should().BeTrue();
        }

        [Theory]
        [InlineData("Coto")]
        [InlineData("COTO 3456")]
        [InlineData("compra coto")]
        [InlineData("Verduleria")]
        [InlineData("Swiss Medical")]
        [InlineData("Litoral Gas")]
        public void IsSystemDetail_ForRealMerchant_ReturnsFalse(string detail)
        {
            MerchantEligibility.IsSystemDetail(detail).Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsSystemDetail_ForEmptyDetail_ReturnsFalse(string? detail)
        {
            // Un detalle vacío no es "del sistema": simplemente no resuelve, y de eso ya se
            // ocupa el resolver devolviendo null.
            MerchantEligibility.IsSystemDetail(detail).Should().BeFalse();
        }
    }
}
