using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.Assistant.Sources;
using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.DTO.PurchaseSpending;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 2 — el método nuevo del servicio de reportes (gasto por categoría a
    // la misma altura del mes) y la fuente income-expense que lo usa.
    public class IncomeExpenseSpendingToDateServiceTests
    {
        private const int UserId = 1;

        private static CategoryToDateSpendingResult Row(int id, string name, decimal current, decimal previous, decimal average, string? parent = null) =>
            new() { CategoryId = id, CategoryName = name, ParentName = parent, CurrentToDate = current, PreviousToDate = previous, AverageToDate = average };

        [Fact]
        public void BuildSpendingToDateDTO_TotalsAreTheSumOfTheCategories_OrderedByCurrentSpend()
        {
            var dto = IncomeExpenseReportService.BuildSpendingToDateDTO(new DateTime(2026, 9, 1), 15, 6, new List<CategoryToDateSpendingResult>
            {
                Row(1, "Delivery", 40.5m, 25m, 30m),
                Row(2, "Supermercado", 100m, 80m, 90m, parent: "Comida"),
                Row(3, "Gimnasio", 0m, 0m, 20m)
            });

            dto.TotalCurrent.Should().Be(140.5m);
            dto.TotalPrevious.Should().Be(105m);
            dto.TotalAverage.Should().Be(140m);
            dto.Categories.Select(c => c.CategoryName).Should().Equal("Supermercado", "Delivery", "Gimnasio");
            dto.Categories[0].ParentName.Should().Be("Comida");
            dto.CutoffDay.Should().Be(15);
            dto.ComparisonMonths.Should().Be(6);
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_ClipsTheCutoffDayToTheLengthOfTheMonth()
        {
            var transactions = new Mock<ITransactionRepository>();
            var assets = new Mock<IAssetRepository>();
            assets.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Asset { Id = 1, Name = "Peso Argentino", AssetTypeId = 1 });
            transactions
                .Setup(r => r.GetSpendingToDateByCategoryAsync(UserId, It.IsAny<Asset>(), new DateTime(2026, 2, 1), 28, 6))
                .ReturnsAsync(new List<CategoryToDateSpendingResult> { Row(1, "Delivery", 10m, 0m, 0m) });
            var sut = new IncomeExpenseReportService(transactions.Object, assets.Object);

            var dto = await sut.GetSpendingToDateByCategoryAsync(UserId, 1, new DateTime(2026, 2, 17), 31);

            dto.CutoffDay.Should().Be(28);
            dto.Month.Should().Be(new DateTime(2026, 2, 1));
            transactions.Verify(r => r.GetSpendingToDateByCategoryAsync(UserId, It.IsAny<Asset>(), new DateTime(2026, 2, 1), 28, 6), Times.Once);
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_WithACutoffDayBelowOne_Throws()
        {
            var sut = new IncomeExpenseReportService(Mock.Of<ITransactionRepository>(), Mock.Of<IAssetRepository>());

            var act = () => sut.GetSpendingToDateByCategoryAsync(UserId, 1, new DateTime(2026, 9, 1), 0);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_WhenTheAssetIsNotACurrency_Throws()
        {
            var assets = new Mock<IAssetRepository>();
            assets.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Asset { Id = 5, Name = "YPF", AssetTypeId = 2 });
            var sut = new IncomeExpenseReportService(Mock.Of<ITransactionRepository>(), assets.Object);

            var act = () => sut.GetSpendingToDateByCategoryAsync(UserId, 5, new DateTime(2026, 9, 1), 15);

            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*moneda*");
        }
    }

    public class AssistantIncomeExpenseSourceTests
    {
        private const int UserId = 1;
        private const int ArsId = 1;
        private const int UsdId = 2;

        private readonly Mock<IIncomeExpenseReportService> _reports = new();
        private readonly Mock<IPurchaseSpendingService> _spending = new();
        private readonly Mock<IAssistantCurrencyResolver> _currencies = new();
        private readonly IncomeExpenseContextSource _sut;

        public AssistantIncomeExpenseSourceTests()
        {
            _currencies.Setup(c => c.GetAsync()).ReturnsAsync(new AssistantCurrencySet(
                new Asset { Id = ArsId, Name = "Peso Argentino", Symbol = "ARS" },
                new Asset { Id = UsdId, Name = "Dolar Estadounidense", Symbol = "USD" }));
            _sut = new IncomeExpenseContextSource(_reports.Object, _spending.Object, _currencies.Object);
        }

        private static IncExpEvolutionPointDTO Point(int year, int month, decimal income, decimal expense) =>
            new() { Month = new DateTime(year, month, 1), Income = income, Expense = expense, Result = income - expense };

        private static SpendingToDateCategoryDTO Cat(int id, string name, decimal current, decimal previous, decimal average, string? parent = null) =>
            new() { CategoryId = id, CategoryName = name, ParentName = parent, CurrentToDate = current, PreviousToDate = previous, AverageToDate = average };

        private static SpendingToDateDTO ToDate(int day, params SpendingToDateCategoryDTO[] categories) => new()
        {
            Month = new DateTime(2026, 9, 1),
            CutoffDay = day,
            ComparisonMonths = 6,
            TotalCurrent = categories.Sum(c => c.CurrentToDate),
            TotalPrevious = categories.Sum(c => c.PreviousToDate),
            TotalAverage = categories.Sum(c => c.AverageToDate),
            Categories = categories.ToList()
        };

        // ── ExtractWindow / BuildTotals ───────────────────────────────────────────────────────

        [Fact]
        public void ExtractWindow_TakesTheMonth_ThePreviousOne_AndTheAverageOfTheSixBeforeIt()
        {
            var points = new[]
            {
                Point(2026, 3, 800_000m, 600_000m), Point(2026, 4, 850_000m, 650_000m), Point(2026, 5, 900_000m, 700_000m),
                Point(2026, 6, 900_000m, 700_000m), Point(2026, 7, 950_000m, 750_000m), Point(2026, 8, 900_000m, 700_000m),
                Point(2026, 9, 1_000_000m, 800_000m)
            };

            var w = IncomeExpenseContextSource.ExtractWindow(points, new DateTime(2026, 9, 1), 6);

            w.Current.Should().Be(new IncomeExpenseContextSource.MonthTotals(1_000_000m, 800_000m));
            w.Previous.Should().Be(new IncomeExpenseContextSource.MonthTotals(900_000m, 700_000m));
            w.Average.Income.Should().BeApproximately(883_333.33m, 0.01m);
            w.Average.Expense.Should().BeApproximately(683_333.33m, 0.01m);
        }

        [Fact]
        public void ExtractWindow_AMonthMissingFromTheSeries_CountsAsZero()
        {
            var points = new[] { Point(2026, 9, 600m, 300m), Point(2026, 8, 600m, 600m) };

            var w = IncomeExpenseContextSource.ExtractWindow(points, new DateTime(2026, 9, 1), 6);

            w.Average.Income.Should().Be(100m);   // 600 / 6
            w.Average.Expense.Should().Be(100m);
        }

        [Fact]
        public void BuildTotals_ComputesTheSavingsRateAndItsVariationInPoints()
        {
            var w = new IncomeExpenseContextSource.MonthWindow(
                new IncomeExpenseContextSource.MonthTotals(1_000_000m, 800_000m),
                new IncomeExpenseContextSource.MonthTotals(900_000m, 700_000m),
                new IncomeExpenseContextSource.MonthTotals(883_333.33m, 683_333.33m));

            var totals = IncomeExpenseContextSource.BuildTotals(w, isPesos: true);

            totals.Income.Current.Should().Be(1_000_000m);
            totals.Income.DeltaVsPrevious.Should().Be(100_000m);
            totals.Income.PctVsPrevious.Should().Be(11.1m);
            totals.Income.Average.Should().Be(883_333m);
            totals.Income.DeltaVsAverage.Should().Be(116_667m);
            totals.Income.PctVsAverage.Should().Be(13.2m);
            totals.Result.Current.Should().Be(200_000m);
            totals.SavingsRate.Current.Should().Be(20.0m);
            totals.SavingsRate.Previous.Should().Be(22.2m);
            totals.SavingsRate.Average.Should().Be(22.6m);
            totals.SavingsRate.DeltaPointsVsPrevious.Should().Be(-2.2m);
            totals.SavingsRate.DeltaPointsVsAverage.Should().Be(-2.6m);
        }

        // ── Cortes de listas largas ───────────────────────────────────────────────────────────

        [Fact]
        public void BuildExpenseCategoryCut_KeepsTheTopCategoriesAndGroupsTheRestInOthers_WithoutLosingAnyAmount()
        {
            var ars = ToDate(30, Enumerable.Range(1, 12).Select(i => Cat(i, $"Cat {i:00}", 1000m - i * 10, 900m, 950m)).ToArray());
            var usd = ToDate(30, Enumerable.Range(1, 12).Select(i => Cat(i, $"Cat {i:00}", (1000m - i * 10) / 1000m, 0.9m, 0.95m)).ToArray());

            var cut = IncomeExpenseContextSource.BuildExpenseCategoryCut(ars, usd, 10);

            cut.Categories.Should().HaveCount(10);
            cut.Categories[0].Name.Should().Be("Cat 01");
            cut.Others.Should().NotBeNull();
            cut.Others!.CategoriesCount.Should().Be(2);
            cut.Others.Ars.Current.Should().Be(890m + 880m);   // Cat 11 y Cat 12

            // La suma de lo que viaja (categorías + otras) es el total del tramo, en las dos monedas.
            (cut.Categories.Sum(c => c.Ars.Current) + cut.Others.Ars.Current).Should().Be(ars.TotalCurrent);
            (cut.Categories.Sum(c => c.Usd.Current) + cut.Others.Usd.Current).Should().BeApproximately(usd.TotalCurrent, 0.05m);
            (cut.Categories.Sum(c => c.Ars.Previous) + cut.Others.Ars.Previous).Should().Be(ars.TotalPrevious);
            (cut.Categories.Sum(c => c.Ars.Average) + cut.Others.Ars.Average).Should().Be(ars.TotalAverage);
        }

        [Fact]
        public void BuildExpenseCategoryCut_WithFewCategories_HasNoOthers()
        {
            var ars = ToDate(30, Cat(1, "Delivery", 100m, 50m, 60m));
            var usd = ToDate(30, Cat(1, "Delivery", 0.1m, 0.05m, 0.06m));

            IncomeExpenseContextSource.BuildExpenseCategoryCut(ars, usd, 10).Others.Should().BeNull();
        }

        [Fact]
        public void BuildExpenseCategoryCut_RanksByTheLargerOfCurrentAndAverage_SoACategoryThatDroppedToZeroSurvives()
        {
            // Gimnasio gastó 0 este mes pero promedia 5.000; Cine gastó 100 y promedia 90.
            var ars = ToDate(30, Cat(1, "Cine", 100m, 90m, 90m), Cat(2, "Gimnasio", 0m, 5000m, 5000m), Cat(3, "Libros", 50m, 40m, 40m));
            var usd = ToDate(30, Cat(1, "Cine", 0.1m, 0.09m, 0.09m), Cat(2, "Gimnasio", 0m, 5m, 5m), Cat(3, "Libros", 0.05m, 0.04m, 0.04m));

            var cut = IncomeExpenseContextSource.BuildExpenseCategoryCut(ars, usd, 2);

            cut.Categories.Select(c => c.Name).Should().Equal("Gimnasio", "Cine");
            cut.Others!.CategoriesCount.Should().Be(1);
        }

        [Fact]
        public void BuildExpenseCategoryCut_TheSameCategoriesTravelInBothCurrencies_WithTheirOwnVariations()
        {
            var ars = ToDate(30, Cat(1, "Delivery", 120m, 100m, 100m));
            var usd = ToDate(30, Cat(1, "Delivery", 0.08m, 0.10m, 0.10m)); // en pesos subió, en dólares bajó

            var entry = IncomeExpenseContextSource.BuildExpenseCategoryCut(ars, usd, 10).Categories.Single();

            entry.Ars.PctVsPrevious.Should().Be(20.0m);
            entry.Usd.PctVsPrevious.Should().Be(-20.0m);
            entry.ShareOfExpensePct.Should().Be(100.0m);
        }

        [Fact]
        public void BuildExpenseCategoryCut_ACategoryOnlyPresentInOneCurrency_GetsZerosInTheOther()
        {
            var ars = ToDate(30, Cat(1, "Delivery", 100m, 0m, 0m));
            var usd = ToDate(30);

            var entry = IncomeExpenseContextSource.BuildExpenseCategoryCut(ars, usd, 10).Categories.Single();

            entry.Usd.Current.Should().Be(0m);
            entry.Usd.PctVsPrevious.Should().BeNull();
        }

        [Fact]
        public void BuildIncomeCut_KeepsTheLargestAndGroupsTheRestInOthers()
        {
            var ars = new IncomeCompositionDTO
            {
                Categories = Enumerable.Range(1, 10).Select(i => new IncomeCategoryAmountDTO { CategoryId = i, CategoryName = $"Ing {i}", Amount = i * 1000m }).ToList()
            };
            var usd = new IncomeCompositionDTO
            {
                Categories = Enumerable.Range(1, 10).Select(i => new IncomeCategoryAmountDTO { CategoryId = i, CategoryName = $"Ing {i}", Amount = i }).ToList()
            };

            var cut = IncomeExpenseContextSource.BuildIncomeCut(ars, usd, 8);

            cut.Items.Should().HaveCount(8);
            cut.Items[0].Name.Should().Be("Ing 10");
            cut.Items[0].SharePct.Should().Be(18.2m); // 10.000 de 55.000
            cut.Others!.CategoriesCount.Should().Be(2);
            cut.Others.Ars.Should().Be(3000m);        // 1.000 + 2.000
            cut.Others.Usd.Should().Be(3m);
            (cut.Items.Sum(i => i.Ars) + cut.Others.Ars).Should().Be(55_000m);
        }

        // ── BuildAsync ────────────────────────────────────────────────────────────────────────

        // Lo que devuelve el servicio de gasto por fecha de compra para una moneda: el tramo, los meses completos y la serie diaria.
        private static PurchaseSpendingWindowDTO Window(SpendingToDateDTO toDate, SpendingToDateDTO fullMonths, params DaySpendingDTO[] days) => new()
        {
            ToDate = toDate,
            FullMonths = fullMonths,
            Days = days.ToList()
        };

        // Mismas monedas que el resolutor: la fuente pide las dos en una sola llamada.
        private void SetUpWindows(DateTime month, int cutoff, PurchaseSpendingWindowDTO ars, PurchaseSpendingWindowDTO usd) =>
            _spending
                .Setup(s => s.GetWindowsAsync(UserId, It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { ArsId, UsdId })), month, cutoff, It.IsAny<int>()))
                .ReturnsAsync(new Dictionary<int, PurchaseSpendingWindowDTO> { [ArsId] = ars, [UsdId] = usd });

        private static SpendingToDateDTO Totals(decimal current, decimal previous, decimal average) => new()
        {
            TotalCurrent = current, TotalPrevious = previous, TotalAverage = average
        };

        private void SetUpMonthClose()
        {
            // El gasto del mes completo, por fecha de compra: 800.000 contra 700.000 el mes anterior y 700.000 de promedio.
            SetUpWindows(new DateTime(2026, 9, 1), 31,
                Window(ToDate(30, Cat(1, "Supermercado", 500_000m, 450_000m, 440_000m, "Comida"), Cat(2, "Delivery", 300_000m, 250_000m, 243_333m)),
                    Totals(800_000m, 700_000m, 700_000m)),
                Window(ToDate(30, Cat(1, "Supermercado", 500m, 450m, 440m, "Comida"), Cat(2, "Delivery", 300m, 250m, 243.33m)),
                    Totals(800m, 700m, 700m)));

            foreach (var (assetId, scale) in new[] { (ArsId, 1m), (UsdId, 0.001m) })
            {
                // De la evolución solo importan los ingresos (el "gasto" de la evolución es el de las cuentas y NO se usa).
                var points = new[]
                {
                    Point(2026, 3, 800_000m * scale, 111m), Point(2026, 4, 850_000m * scale, 111m), Point(2026, 5, 900_000m * scale, 111m),
                    Point(2026, 6, 900_000m * scale, 111m), Point(2026, 7, 950_000m * scale, 111m), Point(2026, 8, 900_000m * scale, 111m),
                    Point(2026, 9, 1_000_000m * scale, 111m)
                };
                _reports.Setup(r => r.GetEvolutionAsync(UserId, assetId, It.IsAny<int>())).ReturnsAsync(points);
                _reports.Setup(r => r.GetIncomeCompositionAsync(UserId, new DateTime(2026, 9, 1), assetId)).ReturnsAsync(new IncomeCompositionDTO
                {
                    Categories = new List<IncomeCategoryAmountDTO>
                    {
                        new() { CategoryId = 10, CategoryName = "Sueldo", Amount = 900_000m * scale },
                        new() { CategoryId = 11, CategoryName = "Otros", Amount = 100_000m * scale }
                    }
                });
            }
        }

        [Fact]
        public async Task BuildAsync_ForAMonthClose_TheSpendingIsByPurchaseDateAndTheIncomeIsTheOneThatEnteredTheAccounts()
        {
            SetUpMonthClose();
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

            var data = (IncomeExpenseContextSource.MonthCloseData)(await _sut.BuildAsync(request))!;

            data.Totals.Ars.Expense.Current.Should().Be(800_000m);   // del servicio por fecha de compra, no de la evolución (111)
            data.Totals.Ars.Expense.Previous.Should().Be(700_000m);
            data.Totals.Ars.Expense.PctVsPrevious.Should().Be(14.3m);
            data.Totals.Usd.Expense.Current.Should().Be(800m);
            data.Totals.Usd.Expense.DeltaVsPrevious.Should().Be(100m);
            data.Totals.Ars.Income.Current.Should().Be(1_000_000m);  // de la evolución, como siempre
            data.ExpensesByCategory.Categories.Select(c => c.Name).Should().Equal("Supermercado", "Delivery");
            data.ExpensesByCategory.Categories[0].Rubro.Should().Be("Comida");
            data.ExpensesByCategory.Categories[0].Ars.Current.Should().Be(500_000m);
            data.ExpensesByCategory.Categories[0].Usd.Current.Should().Be(500m);
            data.IncomeComposition.Items.Select(i => i.Name).Should().Equal("Sueldo", "Otros");
            data.IncomeComposition.Items[0].SharePct.Should().Be(90.0m);
        }

        [Fact]
        public async Task BuildAsync_ForAMonthClose_TheResultAndTheSavingsRateComeFromTheSameMeasureAsTheSpending()
        {
            SetUpMonthClose();
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

            var data = (IncomeExpenseContextSource.MonthCloseData)(await _sut.BuildAsync(request))!;

            data.Totals.Ars.Result.Current.Should().Be(200_000m);   // 1.000.000 de ingresos menos 800.000 de gasto por compra
            data.Totals.Ars.Result.Previous.Should().Be(200_000m);  // 900.000 menos 700.000
            data.Totals.Ars.SavingsRate.Current.Should().Be(20.0m);
        }

        [Fact]
        public async Task BuildAsync_ForAMonthClose_AsksForTheFullMonthsInASingleCallAndNeverReadsTheAccountSpendingOrTheCalendar()
        {
            SetUpMonthClose();
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

            await _sut.BuildAsync(request);

            // Un cierre compara meses completos: corte en 31 (se acota solo a los días de cada mes) y una sola consulta para las dos monedas.
            _spending.Verify(s => s.GetWindowsAsync(UserId, It.IsAny<IReadOnlyList<int>>(), new DateTime(2026, 9, 1), 31, It.IsAny<int>()), Times.Once);
            // Los ingresos: el mes cerrado más los 6 anteriores, al menos 7 puntos de la serie.
            _reports.Verify(r => r.GetEvolutionAsync(UserId, ArsId, It.Is<int>(m => m >= 7)), Times.Once);
            // El gasto de las cuentas y su calendario ya no se usan.
            _reports.Verify(r => r.GetSpendingToDateByCategoryAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>()), Times.Never);
            _reports.Verify(r => r.GetCalendarAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonth_SendsTheSameStretchAndTheProjectionFromTheDailySeriesByPurchaseDate()
        {
            var cutoff = new DateTime(2026, 9, 16);
            var request = new AssistantContextRequest(UserId, AssistantReviewKind.MidMonth, new DateTime(2026, 9, 1), cutoff);

            PurchaseSpendingWindowDTO For(decimal scale) => Window(
                ToDate(16, Cat(1, "Supermercado", 2000m * scale, 1500m * scale, 1800m * scale), Cat(2, "Delivery", 1200m * scale, 1000m * scale, 1000m * scale)),
                Totals(0m, 5000m * scale, 5000m * scale), // meses completos: el mes anterior fue 5.000 y el promedio 5.000
                new DaySpendingDTO { Date = new DateTime(2026, 9, 5), Amount = 1600m * scale },
                new DaySpendingDTO { Date = new DateTime(2026, 9, 10), Amount = 1600m * scale },
                new DaySpendingDTO { Date = new DateTime(2026, 8, 20), Amount = 9999m * scale }); // otro mes: no entra en el acumulado
            SetUpWindows(new DateTime(2026, 9, 1), 16, For(1m), For(0.001m));
            foreach (var assetId in new[] { ArsId, UsdId })
                _reports.Setup(r => r.GetEvolutionAsync(UserId, assetId, It.IsAny<int>())).ReturnsAsync(Array.Empty<IncExpEvolutionPointDTO>());

            var data = (IncomeExpenseContextSource.MidMonthData)(await _sut.BuildAsync(request))!;

            data.CutoffDay.Should().Be(16);
            data.DaysInMonth.Should().Be(30);

            data.ExpenseToDate.Ars.Current.Should().Be(3200m);
            data.ExpenseToDate.Ars.Previous.Should().Be(2500m);
            data.ExpenseToDate.Ars.DeltaVsPrevious.Should().Be(700m);
            data.ExpenseToDate.Ars.PctVsPrevious.Should().Be(28.0m);
            data.ExpenseToDate.Ars.DeltaVsAverage.Should().Be(400m);
            data.ExpenseToDate.Ars.PctVsAverage.Should().Be(14.3m);
            data.ExpenseToDate.Usd.Current.Should().Be(3.20m);

            // 3.200 en 16 días, al ritmo actual cierra en 6.000; el mes anterior completo fue 5.000 y el promedio 5.000.
            data.Projection.Ars.ProjectedMonthEnd.Should().Be(6000m);
            data.Projection.Ars.PreviousMonthTotal.Should().Be(5000m);
            data.Projection.Ars.AverageMonthTotal.Should().Be(5000m);
            data.Projection.Ars.PctVsPreviousMonth.Should().Be(20.0m);
            data.Projection.Ars.PctVsAverage.Should().Be(20.0m);
            data.Projection.Usd.ProjectedMonthEnd.Should().Be(6.00m);
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonth_TheProjectionIsTheSameOneTheThermometerGivesForThatSeries()
        {
            var cutoff = new DateTime(2026, 9, 16);
            var days = new[]
            {
                new DaySpendingDTO { Date = new DateTime(2026, 9, 2), Amount = 123.45m },
                new DaySpendingDTO { Date = new DateTime(2026, 9, 9), Amount = 777.77m },
                new DaySpendingDTO { Date = new DateTime(2026, 9, 20), Amount = 5000m }   // posterior al corte
            };
            var window = Window(ToDate(16), Totals(0m, 0m, 0m), days);
            SetUpWindows(new DateTime(2026, 9, 1), 16, window, window);
            _reports.Setup(r => r.GetEvolutionAsync(UserId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Array.Empty<IncExpEvolutionPointDTO>());

            var data = (IncomeExpenseContextSource.MidMonthData)(await _sut.BuildAsync(
                new AssistantContextRequest(UserId, AssistantReviewKind.MidMonth, new DateTime(2026, 9, 1), cutoff)))!;

            var thermometer = DashboardService.BuildThermometer(days.ToList(), cutoff);
            data.Projection.Ars.ProjectedMonthEnd.Should().Be(AssistantFigures.RoundMoney(thermometer.ProjectedMonthEndAmount, isPesos: true));
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonthInJanuary_NeedsNoSpecialHandlingOfThePreviousYearAndAsksOnce()
        {
            var window = Window(ToDate(10), Totals(0m, 0m, 0m));
            SetUpWindows(new DateTime(2027, 1, 1), 10, window, window);
            _reports.Setup(r => r.GetEvolutionAsync(UserId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Array.Empty<IncExpEvolutionPointDTO>());

            await _sut.BuildAsync(new AssistantContextRequest(UserId, AssistantReviewKind.MidMonth, new DateTime(2027, 1, 1), new DateTime(2027, 1, 10)));

            _spending.Verify(s => s.GetWindowsAsync(UserId, It.IsAny<IReadOnlyList<int>>(), new DateTime(2027, 1, 1), 10, It.IsAny<int>()), Times.Once);
            _reports.Verify(r => r.GetCalendarAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void CombineExpense_TakesTheIncomeFromTheWindowAndTheExpenseFromTheFullMonthsSpending()
        {
            var income = new IncomeExpenseContextSource.MonthWindow(
                new IncomeExpenseContextSource.MonthTotals(1_000m, 999m),
                new IncomeExpenseContextSource.MonthTotals(900m, 999m),
                new IncomeExpenseContextSource.MonthTotals(850m, 999m));

            var combined = IncomeExpenseContextSource.CombineExpense(income, Totals(700m, 600m, 650m));

            combined.Current.Should().Be(new IncomeExpenseContextSource.MonthTotals(1_000m, 700m));
            combined.Previous.Should().Be(new IncomeExpenseContextSource.MonthTotals(900m, 600m));
            combined.Average.Should().Be(new IncomeExpenseContextSource.MonthTotals(850m, 650m));
            combined.Current.Result.Should().Be(300m);
        }

        [Fact]
        public void Measures_SaysThatSpendingIsByPurchaseDate_ThatThePaidStatementDoesNotCount_AndNotToAddItToTheCardsSource()
        {
            _sut.Measures.Should().Contain("fecha de compra").And.Contain("NO suman").And.Contain("cards");
            _sut.Measures.Should().NotContain("cuando se paga la cuota");
            _sut.PrimaryCurrency.Should().Be("ARS");
            _sut.Key.Should().Be("income-expense");
        }
    }
}
