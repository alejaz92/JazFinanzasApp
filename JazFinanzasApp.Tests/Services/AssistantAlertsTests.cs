using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.Assistant.Sources;
using JazFinanzasApp.API.Business.DTO.Assistant;
using JazFinanzasApp.API.Business.DTO.Dashboard;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 10 — alertas por reglas (D8), hallazgos en la bandeja (D8b) y aviso de
    // revisión de calidad (D9). Las reglas son funciones puras: los tests les pasan movimientos armados a mano.
    public class AssistantAlertRulesTests
    {
        private static readonly DateTime Today = new(2026, 10, 17);
        private static readonly AssistantAlertsOptions Options = new();

        private static AlertMovementResult Exp(int category, string name, DateTime date, decimal amount, string? detail = null) =>
            new() { Date = date, MovementType = "E", CategoryId = category, CategoryName = name, AmountInPesos = amount, Detail = detail };

        private static AlertMovementResult Inc(int category, string name, DateTime date, decimal amount = 1_000_000m) =>
            new() { Date = date, MovementType = "I", CategoryId = category, CategoryName = name, AmountInPesos = amount };

        // Seis meses previos (abril a septiembre de 2026) con 600.000 en total en una categoría: promedio 100.000.
        private static List<AlertMovementResult> SixMonthsOf(int category, string name, decimal perMonth) =>
            Enumerable.Range(1, 6).Select(k => Exp(category, name, new DateTime(2026, 10, 1).AddMonths(-k).AddDays(9), perMonth)).ToList();

        [Fact]
        public void TheDefaultThresholdsAreTheOnesApprovedInPhase9()
        {
            Options.SpikeFactor.Should().Be(2.0m);
            Options.SpikeMinAmount.Should().Be(150_000m);
            Options.SpikeHistoryMonths.Should().Be(6);
            Options.LargeFactor.Should().Be(1.25m);
            Options.LargeMinAmount.Should().Be(200_000m);
            Options.LargeLookbackMonths.Should().Be(12);
            Options.LargeMinPreviousCount.Should().Be(3);
            Options.LargeRecentDays.Should().Be(7);
            Options.MissingIncomeMinMonths.Should().Be(5);
            Options.MissingIncomeHistoryMonths.Should().Be(6);
            Options.MissingIncomeGraceDays.Should().Be(5);
            Options.RequiredHistoryMonths.Should().Be(13);
        }

        // ── CategorySpike ──────────────────────────────────────────────────────────────────────

        [Fact]
        public void CategorySpike_FiresAboveTwiceTheAverageAndTheMinimum()
        {
            var movements = SixMonthsOf(1, "Movilidad", 100_000m);
            movements.Add(Exp(1, "Movilidad", new DateTime(2026, 10, 10), 200_001m));

            var alerts = AssistantAlertRules.CategorySpikes(movements, Today, Options);

            var alert = alerts.Should().ContainSingle().Subject;
            alert.Kind.Should().Be("CategorySpike");
            alert.Title.Should().Be("Movilidad");
            alert.LinkId.Should().Be(1);
            alert.Severity.Should().Be("warning");
            alert.AssetSymbol.Should().BeNull(); // el monto sale en Amount, con el formato de la bandeja
            alert.Amount.Should().Be(200_001m);
            alert.Detail.Should().Contain("$100.000").And.Contain("6 meses").And.NotContain("$200.001"); // el monto sale en Amount
        }

        [Fact]
        public void CategorySpike_DoesNotFireExactlyAtTwiceTheAverage()
        {
            // 100.000 de promedio: el límite es estrictamente mayor que 2 veces (y además no llega al mínimo de 150.000).
            var movements = SixMonthsOf(1, "Movilidad", 100_000m);
            movements.Add(Exp(1, "Movilidad", new DateTime(2026, 10, 10), 200_000m));

            AssistantAlertRules.CategorySpikes(movements, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void CategorySpike_NeedsTheMinimumAmountEvenIfTheRatioIsHuge()
        {
            var movements = SixMonthsOf(1, "Ocio", 10_000m);
            movements.Add(Exp(1, "Ocio", new DateTime(2026, 10, 3), 149_999m)); // 15 veces el promedio, bajo el mínimo

            AssistantAlertRules.CategorySpikes(movements, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void CategorySpike_DoesNotFireWithoutHistory()
        {
            var movements = new List<AlertMovementResult> { Exp(1, "Nueva", new DateTime(2026, 10, 3), 900_000m) };

            AssistantAlertRules.CategorySpikes(movements, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void CategorySpike_AddsUpTheMonthAndCountsTodayButNotTheFuture()
        {
            var movements = SixMonthsOf(1, "Delivery", 60_000m); // promedio 60.000
            movements.Add(Exp(1, "Delivery", new DateTime(2026, 10, 5), 70_000m));
            movements.Add(Exp(1, "Delivery", Today, 60_000m));                      // hoy cuenta: 130.000 en total, no llega al mínimo
            movements.Add(Exp(1, "Delivery", Today.AddDays(1), 500_000m));          // mañana no existe todavía

            AssistantAlertRules.CategorySpikes(movements, Today, Options).Should().BeEmpty();

            movements.Add(Exp(1, "Delivery", new DateTime(2026, 10, 12), 30_000m)); // ahora 160.000 > 120.000 y >= 150.000
            AssistantAlertRules.CategorySpikes(movements, Today, Options).Should().ContainSingle();
        }

        [Fact]
        public void CategorySpike_IgnoresIncomeAndOtherCategories()
        {
            var movements = SixMonthsOf(1, "Movilidad", 100_000m);
            movements.Add(Inc(1, "Movilidad", new DateTime(2026, 10, 10), 5_000_000m));
            movements.AddRange(SixMonthsOf(2, "Otra", 100_000m));
            movements.Add(Exp(2, "Otra", new DateTime(2026, 10, 10), 120_000m));

            AssistantAlertRules.CategorySpikes(movements, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void CategorySpike_OrdersTheBiggestFirst()
        {
            var movements = SixMonthsOf(1, "A", 50_000m);
            movements.AddRange(SixMonthsOf(2, "B", 50_000m));
            movements.Add(Exp(1, "A", new DateTime(2026, 10, 2), 200_000m));
            movements.Add(Exp(2, "B", new DateTime(2026, 10, 2), 400_000m));

            AssistantAlertRules.CategorySpikes(movements, Today, Options).Select(a => a.Title).Should().Equal("B", "A");
        }

        // ── LargeMovement ──────────────────────────────────────────────────────────────────────

        private static List<AlertMovementResult> History(int category, string name, params decimal[] amounts) =>
            amounts.Select((a, i) => Exp(category, name, new DateTime(2026, 9, 1).AddDays(-i * 20), a)).ToList();

        [Fact]
        public void LargeMovement_FiresAboveTheLargestPreviousByTheFactorAndTheMinimum()
        {
            var movements = History(1, "Viajes", 40_000m, 100_000m, 160_000m);
            movements.Add(Exp(1, "Viajes", new DateTime(2026, 10, 15), 200_001m, "Pasajes"));

            var alert = AssistantAlertRules.LargeMovements(movements, Today, Options).Should().ContainSingle().Subject;

            alert.Kind.Should().Be("LargeMovement");
            alert.Title.Should().Be("Viajes");
            alert.LinkId.Should().Be(1);
            alert.Date.Should().Be(new DateTime(2026, 10, 15));
            alert.Amount.Should().Be(200_001m);
            alert.Detail.Should().Contain("Pasajes").And.Contain("$160.000").And.NotContain("$200.001"); // el monto sale en Amount
        }

        [Fact]
        public void LargeMovement_DoesNotFireExactlyAtTheFactorTimesTheLargest()
        {
            var movements = History(1, "Viajes", 40_000m, 100_000m, 160_000m);
            movements.Add(Exp(1, "Viajes", new DateTime(2026, 10, 15), 200_000m)); // 1,25 x 160.000, justo en el límite

            AssistantAlertRules.LargeMovements(movements, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void LargeMovement_NeedsTheMinimumAmount()
        {
            var movements = History(1, "Café", 5_000m, 6_000m, 7_000m);
            movements.Add(Exp(1, "Café", new DateTime(2026, 10, 15), 150_000m)); // 21 veces el mayor, pero bajo el mínimo

            AssistantAlertRules.LargeMovements(movements, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void LargeMovement_NeedsAtLeastThreePreviousMovements()
        {
            var movements = History(1, "Viajes", 10_000m, 20_000m); // solo dos previos
            movements.Add(Exp(1, "Viajes", new DateTime(2026, 10, 15), 900_000m));

            AssistantAlertRules.LargeMovements(movements, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void LargeMovement_OnlyShowsTheLastSevenDays()
        {
            var movements = History(1, "Viajes", 40_000m, 100_000m, 160_000m);
            movements.Add(Exp(1, "Viajes", Today.AddDays(-6), 400_000m)); // hace 6 días: el límite, entra
            AssistantAlertRules.LargeMovements(movements, Today, Options).Should().ContainSingle();

            var older = History(1, "Viajes", 40_000m, 100_000m, 160_000m);
            older.Add(Exp(1, "Viajes", Today.AddDays(-7), 400_000m)); // hace 7 días: ya no
            AssistantAlertRules.LargeMovements(older, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void LargeMovement_ComparesAgainstTheLargestAndNotTheAverage_SoMonthlyRentDoesNotFire()
        {
            // El caso real de la Fase 9: "Impuestos y Servicios" mezcla un alquiler de 500.000 con cuentas chicas.
            // Contra el promedio el alquiler dispararía todos los meses; contra el mayor previo, no.
            var movements = new List<AlertMovementResult>();
            for (int k = 1; k <= 6; k++)
            {
                movements.Add(Exp(1, "Impuestos y Servicios", new DateTime(2026, 10, 1).AddMonths(-k).AddDays(4), 500_000m, "Alquiler"));
                movements.Add(Exp(1, "Impuestos y Servicios", new DateTime(2026, 10, 1).AddMonths(-k).AddDays(14), 30_000m, "Luz"));
            }
            movements.Add(Exp(1, "Impuestos y Servicios", new DateTime(2026, 10, 14), 520_000m, "Alquiler"));

            AssistantAlertRules.LargeMovements(movements, Today, Options).Should().BeEmpty();

            // Un alquiler que sube por encima del factor sí avisa.
            movements.Add(Exp(1, "Impuestos y Servicios", new DateTime(2026, 10, 15), 700_000m, "Alquiler nuevo"));
            AssistantAlertRules.LargeMovements(movements, Today, Options).Should().ContainSingle()
                .Which.Detail.Should().Contain("Alquiler nuevo");
        }

        [Fact]
        public void LargeMovement_IgnoresMovementsOlderThanTheLookbackWindow()
        {
            var movements = new List<AlertMovementResult>
            {
                Exp(1, "Viajes", new DateTime(2024, 1, 5), 900_000m), // fuera de los 12 meses previos
                Exp(1, "Viajes", new DateTime(2026, 7, 1), 100_000m),
                Exp(1, "Viajes", new DateTime(2026, 8, 1), 100_000m),
                Exp(1, "Viajes", new DateTime(2026, 9, 1), 100_000m),
                Exp(1, "Viajes", new DateTime(2026, 10, 15), 300_000m)
            };

            AssistantAlertRules.LargeMovements(movements, Today, Options).Should().ContainSingle();
        }

        [Fact]
        public void LargeMovement_LongDetailsAreShortened()
        {
            var movements = History(1, "Viajes", 40_000m, 100_000m, 160_000m);
            movements.Add(Exp(1, "Viajes", new DateTime(2026, 10, 15), 400_000m, new string('x', 200)));

            var alert = AssistantAlertRules.LargeMovements(movements, Today, Options).Single();
            alert.Detail!.Length.Should().BeLessThan(200);
        }

        // ── MissingIncome ──────────────────────────────────────────────────────────────────────

        // Un ingreso por mes, el día `day`, en los `months` meses previos que se indican (1 = septiembre, 2 = agosto...).
        private static List<AlertMovementResult> IncomeOn(int day, params int[] monthsBack) =>
            monthsBack.Select(k => Inc(7, "Sueldo", new DateTime(2026, 10, 1).AddMonths(-k).AddDays(day - 1))).ToList();

        [Fact]
        public void MissingIncome_FiresPastTheUsualDayPlusGraceWhenNothingArrived()
        {
            var movements = IncomeOn(10, 1, 2, 3, 4, 5, 6);

            var alert = AssistantAlertRules.MissingIncome(movements, new DateTime(2026, 10, 16), Options).Should().ContainSingle().Subject;

            alert.Kind.Should().Be("MissingIncome");
            alert.Title.Should().Be("Sueldo");
            alert.LinkId.Should().Be(7);
            alert.Severity.Should().Be("warning");
            alert.Detail.Should().Contain("día 10");
        }

        [Fact]
        public void MissingIncome_DoesNotFireOnTheGraceBoundaryOrBefore()
        {
            var movements = IncomeOn(10, 1, 2, 3, 4, 5, 6);

            AssistantAlertRules.MissingIncome(movements, new DateTime(2026, 10, 15), Options).Should().BeEmpty(); // 10 + 5: todavía no
            AssistantAlertRules.MissingIncome(movements, new DateTime(2026, 10, 16), Options).Should().ContainSingle();
        }

        [Fact]
        public void MissingIncome_DoesNotFireIfTheIncomeArrivedThisMonth()
        {
            var movements = IncomeOn(10, 1, 2, 3, 4, 5, 6);
            movements.Add(Inc(7, "Sueldo", new DateTime(2026, 10, 14)));

            AssistantAlertRules.MissingIncome(movements, Today, Options).Should().BeEmpty();
        }

        [Fact]
        public void MissingIncome_NeedsIncomeInAtLeastFiveOfTheLastSixMonths()
        {
            AssistantAlertRules.MissingIncome(IncomeOn(10, 1, 2, 3, 4, 5), Today, Options).Should().ContainSingle();     // 5 de 6
            AssistantAlertRules.MissingIncome(IncomeOn(10, 1, 2, 3, 4), Today, Options).Should().BeEmpty();               // 4 de 6
            AssistantAlertRules.MissingIncome(IncomeOn(10, 1, 2, 3, 4, 8), Today, Options).Should().BeEmpty();            // el mes 8 queda fuera
        }

        [Fact]
        public void MissingIncome_UsesTheMedianOfTheUsualDaysSoOneOddMonthDoesNotMoveIt()
        {
            var movements = IncomeOn(10, 1, 2, 3, 4, 5);
            movements.AddRange(IncomeOn(28, 6)); // un mes raro, el día 28

            AssistantAlertRules.MissingIncome(movements, new DateTime(2026, 10, 16), Options).Should().ContainSingle()
                .Which.Detail.Should().Contain("día 10");
        }

        [Fact]
        public void MissingIncome_WithTwoIncomesInAMonthUsesTheMonthsMedian()
        {
            // Dos cobros por mes (el 5 y el 25): la mediana mensual es 15, así que avisa pasado el día 20.
            var movements = new List<AlertMovementResult>();
            for (int k = 1; k <= 6; k++)
            {
                movements.Add(Inc(7, "Honorarios", new DateTime(2026, 10, 1).AddMonths(-k).AddDays(4)));
                movements.Add(Inc(7, "Honorarios", new DateTime(2026, 10, 1).AddMonths(-k).AddDays(24)));
            }

            AssistantAlertRules.MissingIncome(movements, new DateTime(2026, 10, 20), Options).Should().BeEmpty();
            AssistantAlertRules.MissingIncome(movements, new DateTime(2026, 10, 21), Options).Should().ContainSingle();
        }

        [Fact]
        public void MissingIncome_CapsTheCheckDayAtTheEndOfTheMonth_SoItNeverFiresForALateUsualDay()
        {
            // Día habitual 28 + 5 pasa de fin de mes: el día de control se acota a fin de mes y recién el mes siguiente
            // se sabe. Es el mismo comportamiento que tuvo la simulación de la Fase 9.
            var movements = IncomeOn(28, 1, 2, 3, 4, 5, 6);

            AssistantAlertRules.MissingIncome(movements, new DateTime(2026, 10, 31), Options).Should().BeEmpty();
        }

        // ── Evaluate: una sola alerta por categoría ────────────────────────────────────────────

        [Fact]
        public void Evaluate_WhenSpikeAndLargeFireForTheSameCategory_KeepsOnlyTheLargeMovement()
        {
            // El caso de hoy en la base real: el mismo "alquiler" mal categorizado dispara las dos.
            var movements = SixMonthsOf(1, "Gym y Deportes", 70_000m);
            movements.Add(Exp(1, "Gym y Deportes", new DateTime(2026, 10, 14), 632_323m, "Alquiler"));

            var alerts = AssistantAlertRules.Evaluate(movements, Today, Options);

            alerts.Should().ContainSingle().Which.Kind.Should().Be("LargeMovement");
        }

        [Fact]
        public void Evaluate_KeepsASpikeOnAnotherCategoryAndTheMissingIncome()
        {
            var movements = SixMonthsOf(1, "Gym y Deportes", 70_000m);
            movements.Add(Exp(1, "Gym y Deportes", new DateTime(2026, 10, 14), 632_323m, "Alquiler"));
            movements.AddRange(SixMonthsOf(2, "Delivery", 60_000m));
            movements.Add(Exp(2, "Delivery", new DateTime(2026, 10, 9), 160_000m)); // pico sin un solo egreso grande
            movements.AddRange(IncomeOn(10, 1, 2, 3, 4, 5, 6));

            AssistantAlertRules.Evaluate(movements, Today, Options).Select(a => a.Kind)
                .Should().BeEquivalentTo("CategorySpike", "LargeMovement", "MissingIncome");
        }

        [Fact]
        public void Evaluate_WithoutMovementsReturnsNothing()
        {
            AssistantAlertRules.Evaluate(new List<AlertMovementResult>(), Today, Options).Should().BeEmpty();
        }
    }

    public class AssistantQualityReviewRuleTests
    {
        private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        private static AssistantQualityReview Review(DateTime at, string before, string after) =>
            new() { ReviewedAt = at, PromptVersionBefore = before, PromptVersionAfter = after };

        [Fact]
        public void IsDueAfterThirtyDaysWithANewReview()
        {
            var last = new[] { Review(Now.AddDays(-30), "v1", "v2") };

            AssistantQualityReviewRule.Evaluate(Now, last, null, 1).Due.Should().BeTrue();
            AssistantQualityReviewRule.Evaluate(Now.AddDays(-1), last, null, 1).Due.Should().BeFalse(); // 29 días
        }

        [Fact]
        public void IsNotDueWithoutANewReviewSinceTheLastOne()
        {
            var last = new[] { Review(Now.AddDays(-45), "v1", "v2") };

            AssistantQualityReviewRule.Evaluate(Now, last, null, 0).Due.Should().BeFalse();
        }

        [Fact]
        public void WhenTheLastTwoReviewsChangedNothingTheIntervalIsNinetyDays()
        {
            var last = new[] { Review(Now.AddDays(-60), "v2", "v2"), Review(Now.AddDays(-120), "v2", "v2") };

            AssistantQualityReviewRule.Evaluate(Now, last, null, 3).Due.Should().BeFalse();
            AssistantQualityReviewRule.Evaluate(Now.AddDays(30), last, null, 3).Due.Should().BeTrue(); // 90 días
        }

        [Fact]
        public void OneReviewThatChangedSomethingKeepsTheIntervalAtThirtyDays()
        {
            var last = new[] { Review(Now.AddDays(-40), "v2", "v2"), Review(Now.AddDays(-80), "v1", "v2") };

            AssistantQualityReviewRule.Evaluate(Now, last, null, 1).Due.Should().BeTrue();
        }

        [Fact]
        public void WithASingleReviewTheIntervalIsThirtyDaysEvenIfItChangedNothing()
        {
            var last = new[] { Review(Now.AddDays(-40), "v1", "v1") };

            AssistantQualityReviewRule.Evaluate(Now, last, null, 1).Due.Should().BeTrue();
        }

        [Fact]
        public void WithoutAnyReviewTheReferenceIsTheFirstFinishedReview()
        {
            AssistantQualityReviewRule.Evaluate(Now, Array.Empty<AssistantQualityReview>(), Now.AddDays(-31), 2).Due.Should().BeTrue();
            AssistantQualityReviewRule.Evaluate(Now, Array.Empty<AssistantQualityReview>(), Now.AddDays(-10), 2).Due.Should().BeFalse();
        }

        [Fact]
        public void WithoutReviewsNorFinishedReviewsItIsNeverDue()
        {
            AssistantQualityReviewRule.Evaluate(Now, Array.Empty<AssistantQualityReview>(), null, 0).Due.Should().BeFalse();
        }

        [Fact]
        public void ReportsTheDaysAndTheNewReviewsForTheMessage()
        {
            var result = AssistantQualityReviewRule.Evaluate(Now, new[] { Review(Now.AddDays(-33), "v1", "v2") }, null, 4);

            result.DaysSince.Should().Be(33);
            result.NewReviews.Should().Be(4);
        }
    }

    // El servicio que arma lo que el asistente le suma a la bandeja: las alertas por reglas para todos, y los
    // hallazgos y el aviso de revisión solo con el asistente habilitado.
    public class AssistantAlertServiceTests
    {
        private const int UserId = 1;
        private static readonly DateTime Today = new(2026, 10, 17);

        private readonly Mock<ITransactionRepository> _transactions = new();
        private readonly Mock<IAssetRepository> _assets = new();
        private readonly Mock<IAssistantUsageService> _usage = new();
        private readonly Mock<IAssistantFindingService> _findings = new();
        private readonly Mock<IAssistantReviewRepository> _reviews = new();
        private readonly AssistantAlertService _sut;

        private static readonly Asset Pesos = new() { Id = 1, Name = "Peso Argentino", Symbol = "$" };

        public AssistantAlertServiceTests()
        {
            _assets.Setup(a => a.GetAssetByNameAsync("Peso Argentino")).ReturnsAsync(Pesos);
            _transactions.Setup(t => t.GetAlertMovementsAsync(UserId, Pesos, It.IsAny<DateTime>()))
                .ReturnsAsync(new List<AlertMovementResult>());
            _usage.Setup(u => u.GetStatusAsync(UserId)).ReturnsAsync(Status(enabled: true));
            _findings.Setup(f => f.GetAsync(UserId, false)).ReturnsAsync(new List<AssistantFindingDTO>());
            _reviews.Setup(r => r.GetLastQualityReviewsAsync(UserId, 2)).ReturnsAsync(new List<AssistantQualityReview>());
            _reviews.Setup(r => r.GetFirstReadyReviewTimeAsync(UserId)).ReturnsAsync((DateTime?)null);

            var time = new Mock<TimeProvider>();
            time.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
            _sut = new AssistantAlertService(
                _transactions.Object, _assets.Object, _usage.Object, _findings.Object, _reviews.Object,
                Options.Create(new AssistantAlertsOptions()), time.Object, NullLogger<AssistantAlertService>.Instance);
        }

        private static AssistantUsageStatus Status(bool enabled) => new(enabled, enabled ? 3m : 0m, 0m, new DateTime(2026, 11, 1));

        private static List<AlertMovementResult> SpikeMovements()
        {
            var list = Enumerable.Range(1, 6).Select(k => new AlertMovementResult
            {
                Date = new DateTime(2026, 10, 1).AddMonths(-k).AddDays(9), MovementType = "E", CategoryId = 5, CategoryName = "Delivery", AmountInPesos = 60_000m
            }).ToList();
            list.Add(new AlertMovementResult { Date = new DateTime(2026, 10, 9), MovementType = "E", CategoryId = 5, CategoryName = "Delivery", AmountInPesos = 160_000m });
            return list;
        }

        private static AssistantFindingDTO Finding(int id = 10, int? categoryId = 5) =>
            new() { Id = id, ReviewId = 99, Title = "Poco gasto en supermercado", Detail = "¿Falta cargar algo?", TransactionClassId = categoryId, Status = "Active", CreatedAt = new DateTime(2026, 10, 15) };

        [Fact]
        public async Task RuleAlertsAreBuiltForEveryUser_EvenWithoutTheAssistant()
        {
            _usage.Setup(u => u.GetStatusAsync(UserId)).ReturnsAsync(Status(enabled: false));
            _transactions.Setup(t => t.GetAlertMovementsAsync(UserId, Pesos, It.IsAny<DateTime>())).ReturnsAsync(SpikeMovements());

            var items = await _sut.BuildAsync(UserId, Today);

            items.Should().ContainSingle().Which.Kind.Should().Be("CategorySpike");
        }

        [Fact]
        public async Task WithoutTheAssistantNeitherFindingsNorTheQualityReviewNoticeAreAdded()
        {
            _usage.Setup(u => u.GetStatusAsync(UserId)).ReturnsAsync(Status(enabled: false));

            var items = await _sut.BuildAsync(UserId, Today);

            items.Should().BeEmpty();
            _findings.Verify(f => f.GetAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            _reviews.Verify(r => r.GetLastQualityReviewsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ActiveFindingsBecomeTrayItemsWithTheirIdAndCategoryLink()
        {
            _findings.Setup(f => f.GetAsync(UserId, false)).ReturnsAsync(new List<AssistantFindingDTO> { Finding(10, 5), Finding(11, null) });

            var items = await _sut.BuildAsync(UserId, Today);

            items.Should().HaveCount(2);
            var first = items[0];
            first.Kind.Should().Be("AssistantFinding");
            first.Title.Should().Be("Poco gasto en supermercado");
            first.Detail.Should().Be("¿Falta cargar algo?");
            first.FindingId.Should().Be(10);
            first.LinkId.Should().Be(5);
            first.Severity.Should().Be("info");
            items[1].LinkId.Should().BeNull();
            items[1].FindingId.Should().Be(11);
        }

        [Fact]
        public async Task TheQualityReviewNoticeAppearsWhenItIsDue()
        {
            _reviews.Setup(r => r.GetLastQualityReviewsAsync(UserId, 2)).ReturnsAsync(new List<AssistantQualityReview>
            {
                new() { ReviewedAt = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), PromptVersionBefore = "v1", PromptVersionAfter = "v2" }
            });
            _reviews.Setup(r => r.CountReadyReviewsSinceAsync(UserId, new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc))).ReturnsAsync(2);

            var items = await _sut.BuildAsync(UserId, Today);

            var notice = items.Should().ContainSingle().Subject;
            notice.Kind.Should().Be("AssistantQualityReviewDue");
            notice.Severity.Should().Be("info");
            notice.Detail.Should().Contain("41 días").And.Contain("2 devoluciones nuevas").And.Contain("sesión de trabajo con Claude");
        }

        [Fact]
        public async Task TheQualityReviewNoticeIsAbsentWhenNotDue()
        {
            _reviews.Setup(r => r.GetLastQualityReviewsAsync(UserId, 2)).ReturnsAsync(new List<AssistantQualityReview>
            {
                new() { ReviewedAt = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc), PromptVersionBefore = "v1", PromptVersionAfter = "v2" }
            });
            _reviews.Setup(r => r.CountReadyReviewsSinceAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(5);

            (await _sut.BuildAsync(UserId, Today)).Should().BeEmpty();
        }

        [Fact]
        public async Task TheFirstFinishedReviewIsTheReferenceWhenNoQualityReviewWasEverRegistered()
        {
            var firstReady = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            _reviews.Setup(r => r.GetFirstReadyReviewTimeAsync(UserId)).ReturnsAsync(firstReady);
            _reviews.Setup(r => r.CountReadyReviewsSinceAsync(UserId, firstReady.AddTicks(-1))).ReturnsAsync(2);

            var items = await _sut.BuildAsync(UserId, Today);

            items.Should().ContainSingle().Which.Kind.Should().Be("AssistantQualityReviewDue");
        }

        [Fact]
        public async Task AFailureInTheRuleAlertsDoesNotTakeDownTheFindings()
        {
            _transactions.Setup(t => t.GetAlertMovementsAsync(UserId, Pesos, It.IsAny<DateTime>())).ThrowsAsync(new InvalidOperationException("boom"));
            _findings.Setup(f => f.GetAsync(UserId, false)).ReturnsAsync(new List<AssistantFindingDTO> { Finding() });

            var items = await _sut.BuildAsync(UserId, Today);

            items.Should().ContainSingle().Which.Kind.Should().Be("AssistantFinding");
        }

        [Fact]
        public async Task AFailureInTheAssistantPartDoesNotTakeDownTheRuleAlerts()
        {
            _transactions.Setup(t => t.GetAlertMovementsAsync(UserId, Pesos, It.IsAny<DateTime>())).ReturnsAsync(SpikeMovements());
            _findings.Setup(f => f.GetAsync(UserId, false)).ThrowsAsync(new InvalidOperationException("boom"));

            var items = await _sut.BuildAsync(UserId, Today);

            items.Should().ContainSingle().Which.Kind.Should().Be("CategorySpike");
        }

        [Fact]
        public async Task WithoutThePesoAssetThereAreNoRuleAlertsAndNothingBreaks()
        {
            _assets.Setup(a => a.GetAssetByNameAsync("Peso Argentino")).ReturnsAsync((Asset)null!);

            (await _sut.BuildAsync(UserId, Today)).Should().BeEmpty();
        }

        [Fact]
        public async Task AsksTheRepositoryForTheLongestWindowThatTheRulesNeed()
        {
            await _sut.BuildAsync(UserId, Today);

            // 13 meses antes del primero del mes en curso: 12 de egresos previos más el mes en curso.
            _transactions.Verify(t => t.GetAlertMovementsAsync(UserId, Pesos, new DateTime(2025, 9, 1)), Times.Once);
        }
    }

    // Lo que viaja al modelo: las alertas y los hallazgos sí, el aviso de revisión de calidad no.
    public class AssistantAlertsSourceWithNewKindsTests
    {
        [Fact]
        public async Task TheQualityReviewNoticeDoesNotTravelToTheModelButFindingsAndRuleAlertsDo()
        {
            var dashboard = new Mock<IDashboardService>();
            dashboard.Setup(d => d.GetDashboardAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new DashboardDTO
            {
                Pending = new List<DashboardPendingItemDTO>
                {
                    new() { Kind = "CategorySpike", Title = "Delivery", Detail = "Llevás $160.000 este mes.", Severity = "warning", LinkId = 5 },
                    new() { Kind = "AssistantFinding", Title = "Poco gasto en supermercado", Severity = "info", FindingId = 10 },
                    new() { Kind = "AssistantQualityReviewDue", Title = "Revisión de calidad del asistente", Severity = "info" }
                }
            });
            var sut = new AlertsContextSource(dashboard.Object, AssistantSourceTestHelpers.Currencies().Object);

            var data = (AlertsContextSource.AlertsData)(await sut.BuildAsync(AssistantSourceTestHelpers.Mid(AssistantSourceTestHelpers.ThisMonth, 16)))!;

            data.Pending.Select(i => i.Kind).Should().Equal("CategorySpike", "AssistantFinding");
            sut.Measures.Should().Contain("AssistantFinding").And.Contain("CategorySpike");
        }
    }

    // La consulta que alimenta las reglas: ingresos y egresos en pesos con las guardas T1/T2.
    public class AssistantAlertMovementsRepositoryTests
    {
        private const int UserId = 1;

        private static ApplicationDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static Transaction Tx(Asset asset, TransactionClass? cls, DateTime date, decimal amount, string type, int userId = UserId) => new()
        {
            UserId = userId, Asset = asset, AccountId = 1, PortfolioId = 1, Date = date, MovementType = type, Amount = amount, QuotePrice = 1m,
            TransactionClass = cls, Detail = "detalle"
        };

        [Fact]
        public async Task ReturnsIncomeAndExpensesInPesosWithPositiveAmountsAndAppliesTheGuards()
        {
            using var context = CreateContext();
            var type = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var ars = new Asset { Name = "Peso Argentino", Symbol = "$", Color = "#000000", AssetType = type };
            context.Accounts.Add(new Account { Id = 1, Name = "Cuenta", UserId = UserId });
            var counted = new TransactionClass { Description = "Delivery", IncExp = "E", CountsAsIncomeExpense = true, UserId = UserId };
            var notCounted = new TransactionClass { Description = "Transferencias", IncExp = "E", CountsAsIncomeExpense = false, UserId = UserId };
            var salary = new TransactionClass { Description = "Sueldo", IncExp = "I", CountsAsIncomeExpense = true, UserId = UserId };
            var from = new DateTime(2025, 9, 1);

            context.Transactions.AddRange(
                Tx(ars, counted, new DateTime(2026, 10, 5), -1500m, "E"),                 // entra, monto positivo
                Tx(ars, salary, new DateTime(2026, 10, 1), 2_000_000m, "I"),              // entra
                Tx(ars, notCounted, new DateTime(2026, 10, 5), -999m, "E"),               // no cuenta como ingreso o egreso
                Tx(ars, null, new DateTime(2026, 10, 5), -999m, "E"),                      // sin categoría
                Tx(ars, counted, new DateTime(2025, 8, 31), -999m, "E"),                   // antes de la ventana
                Tx(ars, counted, new DateTime(2026, 10, 5), -999m, "E", userId: 2),        // de otro usuario
                Tx(ars, counted, new DateTime(2026, 10, 6), 50m, "C"));                    // otro tipo de movimiento
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetAlertMovementsAsync(UserId, ars, from)).OrderBy(m => m.Date).ToList();

            result.Should().HaveCount(2);
            result[0].Should().BeEquivalentTo(new { MovementType = "I", CategoryName = "Sueldo", AmountInPesos = 2_000_000m });
            result[1].Should().BeEquivalentTo(new { MovementType = "E", CategoryName = "Delivery", AmountInPesos = 1500m, Detail = "detalle" });
        }
    }
}
