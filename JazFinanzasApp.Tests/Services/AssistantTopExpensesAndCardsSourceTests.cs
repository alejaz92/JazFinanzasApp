using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.Assistant.Sources;
using JazFinanzasApp.API.Business.DTO.Card;
using JazFinanzasApp.API.Business.DTO.CardReport;
using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 2 — las fuentes top-expenses y cards.
    public class AssistantTopExpensesSourceTests
    {
        private const int UserId = 1;
        private const int ArsId = 1;
        private const int UsdId = 2;

        private readonly Mock<ITransactionRepository> _transactions = new();
        private readonly Mock<IIncomeExpenseReportService> _reports = new();
        private readonly Mock<IAssistantCurrencyResolver> _currencies = new();
        private readonly TopExpensesContextSource _sut;

        public AssistantTopExpensesSourceTests()
        {
            _currencies.Setup(c => c.GetAsync()).ReturnsAsync(new AssistantCurrencySet(
                new Asset { Id = ArsId, Name = "Peso Argentino", Symbol = "ARS" },
                new Asset { Id = UsdId, Name = "Dolar Estadounidense", Symbol = "USD" }));
            _sut = new TopExpensesContextSource(_transactions.Object, _reports.Object, _currencies.Object);
        }

        private static ExpenseMovementResult Movement(int id, string category, string? detail, decimal ars, decimal usd, int day = 10) => new()
        {
            TransactionId = id,
            Date = new DateTime(2026, 9, day),
            CategoryId = id,
            CategoryName = category,
            Detail = detail,
            AmountByAssetId = new Dictionary<int, decimal> { [ArsId] = ars, [UsdId] = usd }
        };

        private static SpendingToDateDTO ToDate(params (int Id, string Name, decimal Current, decimal Average)[] categories) => new()
        {
            Categories = categories.Select(c => new SpendingToDateCategoryDTO { CategoryId = c.Id, CategoryName = c.Name, CurrentToDate = c.Current, AverageToDate = c.Average }).ToList()
        };

        [Fact]
        public void SelectRisingCategories_KeepsOnlyTheOnesAboveTheirAverage_OrderedByHowMuchTheyRose()
        {
            var categories = new[]
            {
                (1, "Delivery", 300m, 100m),     // subió 200
                (2, "Movilidad", 500m, 200m),    // subió 300
                (3, "Gimnasio", 0m, 5000m),      // cayó a cero: no hay movimiento que explicar
                (4, "Cine", 90m, 90m),           // igual al promedio
                (5, "Libros", 50m, 80m),         // bajó
                (6, "Ropa", 160m, 100m),         // subió 60
                (7, "Salud", 400m, 150m)         // subió 250
            };

            var result = TopExpensesContextSource.SelectRisingCategories(categories.Select(c => (c.Item1, c.Item2, c.Item3, c.Item4)), 3);

            result.Select(r => r.CategoryName).Should().Equal("Movilidad", "Salud", "Delivery");
        }

        [Fact]
        public async Task BuildAsync_SendsTheLargestExpensesAndTheMainMovementsOfTheCategoriesThatRoseMost()
        {
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));
            _transactions
                .Setup(r => r.GetLargestExpensesAsync(UserId, It.IsAny<IReadOnlyList<Asset>>(), new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), 10, null))
                .ReturnsAsync(new[] { Movement(1, "Mantenimiento auto", "service", 240_000.4m, 240.36m), Movement(2, "Supermercado", "Coto", 90_000m, 90m) });
            _reports.Setup(r => r.GetSpendingToDateByCategoryAsync(UserId, ArsId, new DateTime(2026, 9, 1), 30))
                .ReturnsAsync(ToDate((1, "Mantenimiento auto", 240_000m, 20_000m), (2, "Supermercado", 90_000m, 95_000m)));
            _transactions
                .Setup(r => r.GetLargestExpensesAsync(UserId, It.IsAny<IReadOnlyList<Asset>>(), new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), 3, 1))
                .ReturnsAsync(new[] { Movement(1, "Mantenimiento auto", "service", 240_000m, 240m) });

            var data = (TopExpensesContextSource.TopExpensesData)(await _sut.BuildAsync(request))!;

            data.From.Should().Be("2026-09-01");
            data.To.Should().Be("2026-09-30");
            data.Largest.Should().HaveCount(2);
            data.Largest[0].Detail.Should().Be("service");
            data.Largest[0].Ars.Should().Be(240_000m);   // redondeado a pesos enteros
            data.Largest[0].Usd.Should().Be(240.36m);
            data.Largest[0].Date.Should().Be("2026-09-10");
            data.CategoriesThatRoseMost.Should().ContainSingle().Which.Category.Should().Be("Mantenimiento auto");
            data.CategoriesThatRoseMost[0].Movements.Should().ContainSingle();
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonth_CutsTheRangeAtTheCutoffDay()
        {
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MidMonth, new DateTime(2026, 9, 1), new DateTime(2026, 9, 16));
            _transactions
                .Setup(r => r.GetLargestExpensesAsync(UserId, It.IsAny<IReadOnlyList<Asset>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(new[] { Movement(1, "Delivery", "pizza", 8000m, 8m) });
            _reports.Setup(r => r.GetSpendingToDateByCategoryAsync(UserId, ArsId, new DateTime(2026, 9, 1), 16)).ReturnsAsync(ToDate());

            var data = (TopExpensesContextSource.TopExpensesData)(await _sut.BuildAsync(request))!;

            data.To.Should().Be("2026-09-16");
            _transactions.Verify(r => r.GetLargestExpensesAsync(UserId, It.IsAny<IReadOnlyList<Asset>>(), new DateTime(2026, 9, 1), new DateTime(2026, 9, 17), 10, null), Times.Once);
        }

        [Fact]
        public async Task BuildAsync_RanksBothCurrenciesAndGivesThePesosFirst()
        {
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));
            IReadOnlyList<Asset>? received = null;
            _transactions
                .Setup(r => r.GetLargestExpensesAsync(UserId, It.IsAny<IReadOnlyList<Asset>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), 10, null))
                .Callback<int, IReadOnlyList<Asset>, DateTime, DateTime, int, int?>((_, assets, _, _, _, _) => received = assets)
                .ReturnsAsync(new[] { Movement(1, "Delivery", "pizza", 8000m, 8m) });
            _reports.Setup(r => r.GetSpendingToDateByCategoryAsync(UserId, ArsId, It.IsAny<DateTime>(), 30)).ReturnsAsync(ToDate());

            await _sut.BuildAsync(request);

            received!.Select(a => a.Id).Should().Equal(ArsId, UsdId);
        }

        [Fact]
        public async Task BuildAsync_WithoutExpensesInThePeriod_HasNothingToSay()
        {
            _transactions
                .Setup(r => r.GetLargestExpensesAsync(UserId, It.IsAny<IReadOnlyList<Asset>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(Array.Empty<ExpenseMovementResult>());

            var data = await _sut.BuildAsync(new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)));

            data.Should().BeNull();
        }

        [Fact]
        public async Task BuildAsync_ShortensLongDetails()
        {
            _transactions
                .Setup(r => r.GetLargestExpensesAsync(UserId, It.IsAny<IReadOnlyList<Asset>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), 10, null))
                .ReturnsAsync(new[] { Movement(1, "Delivery", new string('x', 200), 8000m, 8m) });
            _reports.Setup(r => r.GetSpendingToDateByCategoryAsync(UserId, ArsId, It.IsAny<DateTime>(), 30)).ReturnsAsync(ToDate());

            var data = (TopExpensesContextSource.TopExpensesData)(await _sut.BuildAsync(
                new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30))))!;

            data.Largest[0].Detail.Length.Should().BeLessThanOrEqualTo(81);
        }
    }

    public class AssistantCardsSourceTests
    {
        private const int UserId = 1;
        private const int ArsId = 1;
        private const int UsdId = 2;

        private readonly Mock<ICardReportService> _cardReports = new();
        private readonly Mock<ICardService> _cardService = new();
        private readonly Mock<IAssistantCurrencyResolver> _currencies = new();
        private readonly CardsContextSource _sut;

        public AssistantCardsSourceTests()
        {
            _currencies.Setup(c => c.GetAsync()).ReturnsAsync(new AssistantCurrencySet(
                new Asset { Id = ArsId, Name = "Peso Argentino", Symbol = "ARS" },
                new Asset { Id = UsdId, Name = "Dolar Estadounidense", Symbol = "USD" }));
            _sut = new CardsContextSource(_cardReports.Object, _cardService.Object, _currencies.Object);
        }

        private static CardMonthlySeriesPointDTO SeriesPoint(DateTime month, params (int CardId, string Name, decimal Pesos, decimal Dollars)[] cards) => new()
        {
            Month = month,
            Cards = cards.Select(c => new CardMonthAmountDTO { CardId = c.CardId, CardName = c.Name, PesosAmount = c.Pesos, DollarsAmount = c.Dollars }).ToList()
        };

        // Visa: 100 / 120 / 80 / 90 / 100 / 110 / 100 de marzo a septiembre; Master solo gastó en septiembre.
        private static List<CardMonthlySeriesPointDTO> Series(decimal scale) => new()
        {
            SeriesPoint(new DateTime(2026, 3, 1), (1, "Visa", 100_000m * scale, 0m)),
            SeriesPoint(new DateTime(2026, 4, 1), (1, "Visa", 120_000m * scale, 0m)),
            SeriesPoint(new DateTime(2026, 5, 1), (1, "Visa", 80_000m * scale, 0m)),
            SeriesPoint(new DateTime(2026, 6, 1), (1, "Visa", 90_000m * scale, 0m)),
            SeriesPoint(new DateTime(2026, 7, 1), (1, "Visa", 100_000m * scale, 0m)),
            SeriesPoint(new DateTime(2026, 8, 1), (1, "Visa", 100_000m * scale, 0m)),
            SeriesPoint(new DateTime(2026, 9, 1), (1, "Visa", 90_000m * scale, 20_000m * scale), (2, "Master", 30_000m * scale, 0m))
        };

        private void SetUpGeneral()
        {
            _cardReports.Setup(r => r.GetGeneralAsync(UserId, ArsId)).ReturnsAsync(new CardGeneralReportDTO { MonthlySeries = Series(1m) });
            _cardReports.Setup(r => r.GetGeneralAsync(UserId, UsdId)).ReturnsAsync(new CardGeneralReportDTO { MonthlySeries = Series(0.001m) });
        }

        private static CardFutureCommitmentDTO Future(params (DateTime Month, decimal Amount)[] months) => new()
        {
            MonthlySeries = months.Select(m => new FutureCommitmentMonthDTO
            {
                Month = m.Month,
                Purchases = new List<FutureCommitmentPurchaseAmountDTO> { new() { Amount = m.Amount } }
            }).ToList()
        };

        // ── Funciones puras ───────────────────────────────────────────────────────────────────

        [Fact]
        public void ExtractSeries_AddsPesosAndDollarsOfEachCardAndMonth()
        {
            var series = CardsContextSource.ExtractSeries(Series(1m));

            series[1].Name.Should().Be("Visa");
            series[1].ByMonth[new DateTime(2026, 9, 1)].Should().Be(110_000m);
            series[2].ByMonth[new DateTime(2026, 9, 1)].Should().Be(30_000m);
        }

        [Fact]
        public void BuildCardConsumption_ForAClose_ComparesEachCardAgainstThePreviousMonthAndTheAverage()
        {
            var ars = CardsContextSource.ExtractSeries(Series(1m));
            var usd = CardsContextSource.ExtractSeries(Series(0.001m));

            var result = CardsContextSource.BuildCardConsumption(ars, usd, new DateTime(2026, 9, 1), isMonthClose: true);

            result.ByCard.Select(c => c.Card).Should().Equal("Visa", "Master");
            var visa = (Comparison)result.ByCard[0].Consumption.Ars;
            visa.Current.Should().Be(110_000m);
            visa.Previous.Should().Be(100_000m);
            visa.PctVsPrevious.Should().Be(10.0m);
            visa.Average.Should().Be(98_333m);          // (100+120+80+90+100+100) / 6
            visa.DeltaVsAverage.Should().Be(11_667m);
            ((Comparison)result.ByCard[0].Consumption.Usd).Current.Should().Be(110m);

            // Master no gastó nunca antes: el promedio es cero y no hay porcentaje.
            var master = (Comparison)result.ByCard[1].Consumption.Ars;
            master.Current.Should().Be(30_000m);
            master.PctVsPrevious.Should().BeNull();

            var total = (Comparison)result.Total.Ars;
            total.Current.Should().Be(140_000m);
            total.Previous.Should().Be(100_000m);
        }

        [Fact]
        public void BuildCardConsumption_SerializesTheRuntimeShapeOfEachFigure()
        {
            var ars = CardsContextSource.ExtractSeries(Series(1m));
            var usd = CardsContextSource.ExtractSeries(Series(0.001m));

            var close = System.Text.Json.JsonSerializer.Serialize(
                CardsContextSource.BuildCardConsumption(ars, usd, new DateTime(2026, 9, 1), isMonthClose: true), AssistantContextBuilder.JsonOptions);
            var mid = System.Text.Json.JsonSerializer.Serialize(
                CardsContextSource.BuildCardConsumption(ars, usd, new DateTime(2026, 9, 1), isMonthClose: false), AssistantContextBuilder.JsonOptions);

            close.Should().Contain("\"pctVsPrevious\":10").And.Contain("\"deltaVsAverage\"");
            mid.Should().Contain("\"currentToDate\":110000").And.Contain("\"previousMonthFull\"").And.NotContain("pctVs");
        }

        [Fact]
        public void BuildCardConsumption_ForAMidMonth_DoesNotCompareAPartialMonthAgainstFullOnes()
        {
            var ars = CardsContextSource.ExtractSeries(Series(1m));
            var usd = CardsContextSource.ExtractSeries(Series(0.001m));

            var result = CardsContextSource.BuildCardConsumption(ars, usd, new DateTime(2026, 9, 1), isMonthClose: false);

            var visa = result.ByCard[0].Consumption.Ars.Should().BeOfType<CardsContextSource.PartialConsumption>().Subject;
            visa.CurrentToDate.Should().Be(110_000m);
            visa.PreviousMonthFull.Should().Be(100_000m);
            visa.AverageMonthFull.Should().Be(98_333m);
        }

        [Fact]
        public void BuildCardConsumption_LeavesOutCardsWithoutAnyConsumption()
        {
            var ars = CardsContextSource.ExtractSeries(new[] { SeriesPoint(new DateTime(2026, 9, 1), (1, "Visa", 0m, 0m), (2, "Master", 10m, 0m)) });
            var usd = CardsContextSource.ExtractSeries(new[] { SeriesPoint(new DateTime(2026, 9, 1), (1, "Visa", 0m, 0m), (2, "Master", 0m, 0m)) });

            var result = CardsContextSource.BuildCardConsumption(ars, usd, new DateTime(2026, 9, 1), isMonthClose: true);

            result.ByCard.Select(c => c.Card).Should().Equal("Master");
        }

        [Fact]
        public void BuildFutureMonths_SplitsInstallmentsFromRecurrings_ByDifferenceWithTheInstallmentsOnlyReport()
        {
            var oct = new DateTime(2026, 10, 1);
            var nov = new DateTime(2026, 11, 1);

            var months = CardsContextSource.BuildFutureMonths(
                arsAll: Future((oct, 50_000m), (nov, 40_000m)), arsInstallments: Future((oct, 30_000m), (nov, 10_000m)),
                usdAll: Future((oct, 50m), (nov, 40m)), usdInstallments: Future((oct, 30m), (nov, 10m)),
                month: new DateTime(2026, 9, 1), months: 3);

            months.Should().HaveCount(2);
            months[0].Month.Should().Be("2026-10");
            months[0].Installments.Ars.Should().Be(30_000m);
            months[0].Recurring.Ars.Should().Be(20_000m);
            months[0].Recurring.Usd.Should().Be(20m);
            months[1].Recurring.Ars.Should().Be(30_000m);
        }

        [Fact]
        public void BuildFutureMonths_OnlyLooksAtTheThreeMonthsAfterTheClosedOne()
        {
            var months = CardsContextSource.BuildFutureMonths(
                Future((new DateTime(2026, 10, 1), 1m), (new DateTime(2026, 12, 1), 2m), (new DateTime(2027, 1, 1), 3m)), Future(),
                Future(), Future(),
                new DateTime(2026, 9, 1), 3);

            months.Select(m => m.Month).Should().Equal("2026-10", "2026-12");
        }

        // ── BuildAsync ────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task BuildAsync_ForTheLastClosedMonth_SendsTheConsumptionAndTheFutureCommitment()
        {
            var lastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            SetUpGeneral();
            _cardReports.Setup(r => r.GetFutureCommitmentAsync(UserId, It.IsAny<int>(), true, 0))
                .ReturnsAsync(Future((lastMonth.AddMonths(1), 50m)));
            _cardReports.Setup(r => r.GetFutureCommitmentAsync(UserId, It.IsAny<int>(), false, 0))
                .ReturnsAsync(Future((lastMonth.AddMonths(1), 20m)));

            // El pedido es para el mes anterior al actual, así que la serie de prueba (septiembre) no coincide;
            // lo que importa acá es que el compromiso futuro viaje.
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, lastMonth, lastMonth.AddMonths(1).AddDays(-1));
            var data = (CardsContextSource.MonthCloseCardsData)(await _sut.BuildAsync(request))!;

            data.FutureCommitment.Should().ContainSingle();
            data.FutureCommitment[0].Recurring.Ars.Should().Be(30m);
        }

        [Fact]
        public async Task BuildAsync_ForAnOldClose_DoesNotSendTheFutureCommitment_BecauseItWouldDescribeTodaysFuture()
        {
            var oldMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
            SetUpGeneral();
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, oldMonth, oldMonth.AddMonths(1).AddDays(-1));

            var data = (CardsContextSource.MonthCloseCardsData)(await _sut.BuildAsync(request))!;

            data.FutureCommitment.Should().BeEmpty();
            _cardReports.Verify(r => r.GetFutureCommitmentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonth_ListsTheCardsThatFallDueBeforeTheEndOfTheMonth()
        {
            SetUpGeneral();
            _cardService.Setup(s => s.GetAllForUserAsync(UserId)).ReturnsAsync(new[]
            {
                new CardDTO { Id = 1, Name = "Visa", NextDueDate = new DateTime(2026, 9, 25) },     // la segunda quincena
                new CardDTO { Id = 2, Name = "Master", NextDueDate = new DateTime(2026, 9, 10) },   // ya venció
                new CardDTO { Id = 3, Name = "Amex", NextDueDate = new DateTime(2026, 10, 5) },     // del mes que viene
                new CardDTO { Id = 4, Name = "Naranja", NextDueDate = null }
            });
            _cardReports.Setup(r => r.GetMonthSummaryAsync(UserId, new DateTime(2026, 9, 1), 1)).ReturnsAsync(new List<CardTransactionPaymentListDTO>
            {
                new() { CardId = 1, ValueInPesos = 40_000.4m },
                new() { CardId = 1, ValueInPesos = 10_000m }
            });
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MidMonth, new DateTime(2026, 9, 1), new DateTime(2026, 9, 16));

            var data = (CardsContextSource.MidMonthCardsData)(await _sut.BuildAsync(request))!;

            var due = data.DueSoon.Should().ContainSingle().Subject;
            due.Card.Should().Be("Visa");
            due.DueDate.Should().Be("2026-09-25");
            due.AmountPesos.Should().Be(50_000m);
            _cardReports.Verify(r => r.GetMonthSummaryAsync(UserId, It.IsAny<DateTime>(), 2), Times.Never);
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonthWithoutConsumption_HasNothingToSay()
        {
            _cardReports.Setup(r => r.GetGeneralAsync(UserId, It.IsAny<int>())).ReturnsAsync(new CardGeneralReportDTO());
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MidMonth, new DateTime(2026, 9, 1), new DateTime(2026, 9, 16));

            (await _sut.BuildAsync(request)).Should().BeNull();
        }

        [Fact]
        public void Measures_SaysThatItMeasuresPurchasesNotPayments_AndNotToAddItToIncomeExpense()
        {
            _sut.Measures.Should().Contain("mes de la compra").And.Contain("No se suma con income-expense");
            _sut.Key.Should().Be("cards");
        }
    }
}
