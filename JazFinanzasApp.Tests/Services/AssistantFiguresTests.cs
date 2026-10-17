using System.Text.Json;
using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Domain;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 2 — los helpers de cifras (comparaciones ya calculadas, para que el
    // asistente nunca reste ni saque porcentajes) y el armado del contexto.
    public class AssistantFiguresTests
    {
        [Theory]
        [InlineData(2.5, 3)]
        [InlineData(-2.5, -3)]
        [InlineData(1234.4, 1234)]
        public void RoundMoney_InPesos_HasNoDecimalsAndRoundsAwayFromZero(double value, double expected)
        {
            AssistantFigures.RoundMoney((decimal)value, isPesos: true).Should().Be((decimal)expected);
        }

        [Fact]
        public void RoundMoney_InDollars_KeepsTwoDecimals()
        {
            AssistantFigures.RoundMoney(32.945m, isPesos: false).Should().Be(32.95m);
            AssistantFigures.RoundMoney(32.944m, isPesos: false).Should().Be(32.94m);
        }

        [Fact]
        public void Compare_ComputesEveryVariationFromTheUnroundedValues()
        {
            var c = AssistantFigures.Compare(120m, 100m, 80m, isPesos: true);

            c.Current.Should().Be(120m);
            c.DeltaVsPrevious.Should().Be(20m);
            c.PctVsPrevious.Should().Be(20.0m);
            c.DeltaVsAverage.Should().Be(40m);
            c.PctVsAverage.Should().Be(50.0m);
        }

        [Fact]
        public void Compare_RoundsThePercentageToOneDecimal()
        {
            // 400 / 2800 = 14,2857 %
            AssistantFigures.Compare(3200m, 2500m, 2800m, isPesos: true).PctVsAverage.Should().Be(14.3m);
        }

        [Fact]
        public void Compare_DeltaIsComputedBeforeRounding()
        {
            // 10,4 - 0,4 = 10,0 aunque cada término por separado se redondea a 10 y 0.
            var c = AssistantFigures.Compare(10.4m, 0.4m, 0.4m, isPesos: true);

            c.Current.Should().Be(10m);
            c.Previous.Should().Be(0m);
            c.DeltaVsPrevious.Should().Be(10m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Pct_WhenTheBaseIsNotPositive_IsNull(double baseValue)
        {
            AssistantFigures.Pct(50m, (decimal)baseValue).Should().BeNull();
        }

        [Fact]
        public void Compare_OfANegativeResult_HasNoPercentage()
        {
            var c = AssistantFigures.Compare(-50m, -100m, -80m, isPesos: true);

            c.DeltaVsPrevious.Should().Be(50m);
            c.PctVsPrevious.Should().BeNull();
            c.PctVsAverage.Should().BeNull();
        }

        [Fact]
        public void Rate_IsNullWithoutIncome_AndOtherwiseResultOverIncome()
        {
            AssistantFigures.Rate(100m, 0m).Should().BeNull();
            AssistantFigures.Rate(200m, 1000m).Should().Be(20.0m);
        }

        [Fact]
        public void CompareRate_GivesTheVariationInPercentagePoints()
        {
            var r = AssistantFigures.CompareRate(20.0m, 22.2m, 22.6m);

            r.DeltaPointsVsPrevious.Should().Be(-2.2m);
            r.DeltaPointsVsAverage.Should().Be(-2.6m);
        }

        [Fact]
        public void CompareRate_WithAMissingTerm_LeavesThatVariationNull()
        {
            var r = AssistantFigures.CompareRate(20.0m, null, 22.6m);

            r.DeltaPointsVsPrevious.Should().BeNull();
            r.DeltaPointsVsAverage.Should().Be(-2.6m);
        }

        [Fact]
        public void ShortenDetail_TrimsAndCutsLongText()
        {
            AssistantFigures.ShortenDetail("  service  ").Should().Be("service");
            AssistantFigures.ShortenDetail(null).Should().BeEmpty();
            AssistantFigures.ShortenDetail(new string('a', 100), 10).Should().Be(new string('a', 10) + "…");
        }
    }

    public class AssistantContextBuilderTests
    {
        private static AssistantContextRequest CloseRequest() =>
            new(1, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

        private static Mock<IAssistantContextSource> Source(string key, object? data)
        {
            var mock = new Mock<IAssistantContextSource>();
            mock.SetupGet(s => s.Key).Returns(key);
            mock.SetupGet(s => s.Measures).Returns($"mide {key}");
            mock.SetupGet(s => s.PrimaryCurrency).Returns("ARS");
            mock.Setup(s => s.BuildAsync(It.IsAny<AssistantContextRequest>())).ReturnsAsync(data);
            return mock;
        }

        [Fact]
        public async Task BuildAsync_JoinsTheSourcesInRegistrationOrder_AndSkipsTheOnesWithNothingToSay()
        {
            var builder = new AssistantContextBuilder(new[]
            {
                Source("a", new { x = 1 }).Object,
                Source("b", null).Object,
                Source("c", new { y = 2 }).Object
            });

            var context = await builder.BuildAsync(CloseRequest());

            context.Sources.Select(s => s.Key).Should().Equal("a", "c");
        }

        [Fact]
        public async Task BuildAsync_DescribesThePeriod()
        {
            var builder = new AssistantContextBuilder(Array.Empty<IAssistantContextSource>());

            var close = await builder.BuildAsync(CloseRequest());
            var mid = await builder.BuildAsync(new AssistantContextRequest(1, AssistantReviewKind.MidMonth, new DateTime(2026, 10, 1), new DateTime(2026, 10, 16)));

            close.Period.Should().Be(new AssistantPeriod("MonthClose", "2026-09", "2026-09-30", 30, 30, true));
            mid.Period.Should().Be(new AssistantPeriod("MidMonth", "2026-10", "2026-10-16", 31, 16, false));
        }

        [Fact]
        public async Task BuildAsync_ForAWeeklyScan_AnalyzesTheMonthOfTheCutoffDate()
        {
            var builder = new AssistantContextBuilder(Array.Empty<IAssistantContextSource>());

            // El lunes de la semana cae en el mes anterior, pero los datos llegan hasta el 3 de octubre.
            var context = await builder.BuildAsync(new AssistantContextRequest(1, AssistantReviewKind.WeeklyScan, new DateTime(2026, 9, 28), new DateTime(2026, 10, 3)));

            context.Period.Month.Should().Be("2026-10");
            context.Period.DaysElapsed.Should().Be(3);
        }

        [Fact]
        public async Task BuildAsync_WhenASourceFails_TheErrorPropagates()
        {
            var failing = new Mock<IAssistantContextSource>();
            failing.Setup(s => s.BuildAsync(It.IsAny<AssistantContextRequest>())).ThrowsAsync(new InvalidOperationException("sin reporte"));
            var builder = new AssistantContextBuilder(new[] { failing.Object });

            var act = () => builder.BuildAsync(CloseRequest());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("sin reporte");
        }

        [Fact]
        public async Task Serialize_UsesCamelCase_KeepsAccents_AndCarriesTheMeasures()
        {
            var builder = new AssistantContextBuilder(new[] { Source("income-expense", new PerCurrency<decimal>(1500m, 1.5m)).Object });
            var context = await builder.BuildAsync(CloseRequest());

            var json = AssistantContextBuilder.Serialize(context);

            using var doc = JsonDocument.Parse(json);
            var source = doc.RootElement.GetProperty("sources")[0];
            source.GetProperty("key").GetString().Should().Be("income-expense");
            source.GetProperty("measures").GetString().Should().Be("mide income-expense");
            source.GetProperty("primaryCurrency").GetString().Should().Be("ARS");
            source.GetProperty("data").GetProperty("ars").GetDecimal().Should().Be(1500m);
            source.GetProperty("data").GetProperty("usd").GetDecimal().Should().Be(1.5m);
            doc.RootElement.GetProperty("period").GetProperty("isCompleteMonth").GetBoolean().Should().BeTrue();
            json.Should().NotContain("\\u");
        }

        [Fact]
        public async Task Serialize_OmitsNullFields()
        {
            var builder = new AssistantContextBuilder(new[] { Source("x", new Comparison(10m, 0m, 0m, 10m, null, 10m, null)).Object });

            var json = AssistantContextBuilder.Serialize(await builder.BuildAsync(CloseRequest()));

            json.Should().NotContain("pctVsPrevious");
            json.Should().Contain("deltaVsPrevious");
        }

        [Fact]
        public async Task CurrencyResolver_FindsPesosAndDollarsByAssetName()
        {
            var assets = new Mock<API.Infrastructure.Interfaces.IAssetRepository>();
            assets.Setup(r => r.GetAssetByNameAsync("Peso Argentino")).ReturnsAsync(new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" });
            assets.Setup(r => r.GetAssetByNameAsync("Dolar Estadounidense")).ReturnsAsync(new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" });

            var set = await new AssistantCurrencyResolver(assets.Object).GetAsync();

            set.Ars.Id.Should().Be(1);
            set.Usd.Id.Should().Be(2);
        }

        [Fact]
        public async Task CurrencyResolver_WhenACurrencyIsMissing_Throws()
        {
            var assets = new Mock<API.Infrastructure.Interfaces.IAssetRepository>();
            assets.Setup(r => r.GetAssetByNameAsync("Peso Argentino")).ReturnsAsync(new Asset { Id = 1, Name = "Peso Argentino" });
            assets.Setup(r => r.GetAssetByNameAsync("Dolar Estadounidense")).ReturnsAsync((Asset)null!);

            var act = () => new AssistantCurrencyResolver(assets.Object).GetAsync();

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Dolar*");
        }
    }
}
