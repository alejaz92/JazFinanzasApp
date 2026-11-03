using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm-v2.md, Fase 6 — instrucciones v2 y formato de moneda (D8).
    public class AssistantPromptsV2Tests
    {
        public static IEnumerable<object[]> Kinds() => new[]
        {
            new object[] { AssistantReviewKind.MonthClose },
            new object[] { AssistantReviewKind.MidMonth },
            new object[] { AssistantReviewKind.WeeklyScan }
        };

        [Theory]
        [MemberData(nameof(Kinds))]
        public void EveryKindHasAV2ThatResolvesButTheCurrentVersionIsStillV1(string kind)
        {
            AssistantPrompts.ResolveVersion(kind, "v2").Should().EndWith(".v2");
            AssistantPrompts.Exists(AssistantPrompts.ResolveVersion(kind, "v2")).Should().BeTrue();
            // Quedan sin ser vigentes hasta la comparación del pase (Fase 12).
            AssistantPrompts.CurrentVersion(kind).Should().EndWith(".v1");
        }

        [Theory]
        [MemberData(nameof(Kinds))]
        public void TheV2AsksForExplicitCurrenciesInBothAndNeverTheDollarSign(string kind)
        {
            var text = AssistantPrompts.Load(AssistantPrompts.ResolveVersion(kind, "v2"));

            text.Should().Contain("ARS 2.284.215").And.Contain("USD 17.019,22");
            text.Should().Contain("nunca con el símbolo `$`");
            text.Should().Contain("Cada monto va en las dos monedas");
            text.Should().NotContain("US$ 32,94").And.NotContain("`$2.284.215`");
        }

        [Theory]
        [MemberData(nameof(Kinds))]
        public void TheV2ExplainsThePurchaseDateMeasureAndTheTwoCardReadings(string kind)
        {
            var text = AssistantPrompts.Load(AssistantPrompts.ResolveVersion(kind, "v2"));

            text.Should().Contain("fecha de compra");
            text.Should().Contain("installmentsDueThisMonth");
            text.Should().NotContain("cuenta las cuotas de tarjeta cuando se pagan");
        }

        [Fact]
        public void OnlyTheCloseNamesTheMissingData()
        {
            AssistantPrompts.Load("month-close.v2").Should().Contain("dataNotes");
            AssistantPrompts.Load("mid-month.v2").Should().NotContain("dataNotes");
            AssistantPrompts.Load("weekly-scan.v2").Should().NotContain("dataNotes");
        }

        [Fact]
        public void TheV2KeepsTheSameOutputContractAsTheV1()
        {
            AssistantPrompts.Load("month-close.v2").Should().Contain("`headline`").And.Contain("`highlights`").And.Contain("`body`").And.Contain("`findings`");
            AssistantPrompts.Load("mid-month.v2").Should().Contain("`headline`").And.Contain("`highlights`").And.Contain("`body`").And.Contain("`findings`");
            AssistantPrompts.Load("weekly-scan.v2").Should().Contain("`findings`").And.NotContain("`headline`");
        }
    }

    public class AssistantFigureCheckerCurrencyCodeTests
    {
        [Fact]
        public void AmountsWithArsAndUsdAreCheckedLikeTheOnesWithTheSymbol()
        {
            var context = new[] { 2_284_215m, 17_019.22m };

            AssistantFigureChecker.FindUnverified("Gastaste ARS 2.284.215 (USD 17.019,22).", context).Should().BeEmpty();
            AssistantFigureChecker.FindUnverified("Gastaste ARS 2.300.000 (USD 17.019,22).", context).Should().Equal("ARS 2.300.000");
        }

        [Fact]
        public void AnAmountWithACurrencyCodeIsCheckedEvenWhenItHasOneDigit()
        {
            AssistantFigureChecker.FindUnverified("Sumó USD 5.", new[] { 7m }).Should().Equal("USD 5");
            AssistantFigureChecker.FindUnverified("Sumó USD 5.", new[] { 5m }).Should().BeEmpty();
        }

        [Fact]
        public void TheOldSymbolsStillWork()
        {
            AssistantFigureChecker.FindUnverified("Gastaste $85.000 y US$ 32,94.", new[] { 85_000m, 32.94m }).Should().BeEmpty();
        }
    }
}
