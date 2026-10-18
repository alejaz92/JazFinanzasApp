using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 7 — control de cifras (D7) con casos de formato argentino: miles, decimales,
    // US$, % y tolerancia de redondeo.
    public class AssistantFigureCheckerTests
    {
        private const string Context = """
            {
              "period": { "kind": "MonthClose", "month": "2026-09", "cutoffDate": "2026-09-30", "daysInMonth": 30 },
              "sources": [
                { "key": "income-expense", "data": {
                    "expenses": { "ars": { "current": 2284215, "pctVsPrevious": 8.9, "pctVsAverage": -9.9 }, "usd": { "current": 1634.52 } },
                    "savingRate": { "current": -3.3 },
                    "categories": [ { "name": "Delivery", "ars": 85432 }, { "name": "Auto", "ars": 240000 } ]
                } },
                { "key": "alerts", "data": { "items": [ { "title": "Vence la Visa", "detail": "Resumen de $28.400 con vencimiento el 10/10" } ] } }
              ]
            }
            """;

        private static IReadOnlyList<string> Check(string text) => AssistantFigureChecker.FindUnverified(text, Context);

        [Theory]
        [InlineData("El egreso fue de $2.284.215 en el mes.")]
        [InlineData("En dólares, US$ 1.634,52 de egresos.")]
        [InlineData("US$ 1.634,52")]
        [InlineData("Subió 8,9 % contra agosto.")]
        [InlineData("Fue 9,9 % menos que el promedio.")]
        [InlineData("La tasa de ahorro fue de −3,3 %.")]
        [InlineData("Delivery gastó $85.000.")]            // redondeado al millar: 85.432
        [InlineData("El service del auto costó $240.000.")]
        [InlineData("Un resumen de $28.400 que vence pronto.")]  // número dentro de un texto del contexto
        public void FiguresThatAreInTheContextAreVerified(string text)
        {
            Check(text).Should().BeEmpty();
        }

        [Fact]
        public void APercentageRoundedToAnIntegerIsVerified()
        {
            // 8,9 → "9 %": media unidad del último dígito escrito.
            Check("Subió 9 %.").Should().BeEmpty();
        }

        [Theory]
        [InlineData("El egreso fue de $2.284.216.", "$2.284.216")]   // un peso de diferencia en un monto exacto
        [InlineData("Gastó $999.999 en total.", "$999.999")]
        [InlineData("Subió 12,5 %.", "12,5 %")]
        [InlineData("En dólares, US$ 99,99.", "US$ 99,99")]
        [InlineData("Representa el 44 % del total.", "44 %")]
        public void FiguresThatAreNotInTheContextAreReported(string text, string expected)
        {
            Check(text).Should().ContainSingle().Which.Should().Be(expected);
        }

        [Fact]
        public void AScaledAmountIsComparedWithTheToleranceOfItsLastDigit()
        {
            Check("Los egresos fueron de $2,3 millones.").Should().BeEmpty();   // 2.284.215 ≈ 2,3 millones
            Check("Los egresos fueron de $3,3 millones.").Should().ContainSingle();
        }

        [Theory]
        [InlineData("Cerró en septiembre de 2026.")]
        [InlineData("Los datos llegan al 30/09 y al 30/09/2026.")]
        [InlineData("Período 2026-09, corte 2026-09-30.")]
        [InlineData("Vence el 15 de octubre.")]
        [InlineData("En los últimos 12 meses y 30 días.")]
        [InlineData("El doble que el promedio y tres veces más.")]
        [InlineData("Fueron 3 categorías.")]            // número de un solo dígito sin símbolo
        public void DatesYearsTimeSpansAndExpressionsAreNotChecked(string text)
        {
            Check(text).Should().BeEmpty();
        }

        [Fact]
        public void ASingleDigitIsCheckedWhenItIsAPercentageOrAnAmount()
        {
            Check("Subió 7 %.").Should().ContainSingle().Which.Should().Be("7 %");
            Check("Costó $7.").Should().ContainSingle().Which.Should().Be("$7");
        }

        [Fact]
        public void TheSameFigureIsReportedOnlyOnce()
        {
            Check("Gastó $999.999. Otra vez $999.999.").Should().ContainSingle();
        }

        [Fact]
        public void EveryUnverifiedFigureOfATextIsReported()
        {
            Check("Gastó $111.111 y $222.222, con 77 % de avance.")
                .Should().BeEquivalentTo("$111.111", "$222.222", "77 %");
        }

        [Fact]
        public void TheContextNumbersIncludeNumericValuesAndNumbersInsideStrings()
        {
            var numbers = AssistantFigureChecker.ExtractContextNumbers(Context);

            numbers.Should().Contain(new[] { 2284215m, 8.9m, 9.9m, 3.3m, 1634.52m, 85432m, 28400m, 2026m, 30m });
        }

        [Fact]
        public void AnEmptyTextHasNoUnverifiedFigures()
        {
            Check("").Should().BeEmpty();
        }
    }
}
