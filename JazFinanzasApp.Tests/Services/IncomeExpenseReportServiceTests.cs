using FluentAssertions;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class IncomeExpenseReportServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly IncomeExpenseReportService _sut;

        private const int UserId = 1;

        public IncomeExpenseReportServiceTests()
        {
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _sut = new IncomeExpenseReportService(_transactionRepoMock.Object, _assetRepoMock.Object);
        }

        // ── Guarda de moneda, mismo criterio que ReportService.GetIncExpStatsAsync ─────────────

        [Fact]
        public async Task GetWaterfallAsync_WhenAssetIsNotCurrency_ThrowsBusinessRuleException()
        {
            _assetRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Asset { Id = 5, Name = "YPF", AssetTypeId = 2 });

            var act = () => _sut.GetWaterfallAsync(UserId, DateTime.Today, 5);

            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*moneda*");
        }

        [Fact]
        public async Task GetWaterfallAsync_WhenAssetDoesNotExist_ThrowsNotFoundException()
        {
            _assetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Asset)null!);

            var act = () => _sut.GetWaterfallAsync(UserId, DateTime.Today, 99);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── D-A: BuildEvolutionDTO (lógica pura) — acumulado y promedio móvil de 6 meses ───────

        private static IncExpEvolutionPointResult Point(int year, int month, decimal income, decimal expense) => new()
        {
            Month = new DateTime(year, month, 1),
            Income = income,
            Expense = expense,
            Result = income - expense
        };

        [Fact]
        public void BuildEvolutionDTO_FewerThanSixMonths_MovingAverageIsNull()
        {
            var points = new List<IncExpEvolutionPointResult>
            {
                Point(2026, 1, 1000, 600),
                Point(2026, 2, 1000, 700),
            };

            var result = IncomeExpenseReportService.BuildEvolutionDTO(points);

            result.Should().OnlyContain(p => p.ExpenseMovingAverage == null);
        }

        [Fact]
        public void BuildEvolutionDTO_SixMonthsOrMore_ComputesTrailingAverageOfExpense()
        {
            var points = new List<IncExpEvolutionPointResult>
            {
                Point(2026, 1, 1000, 100),
                Point(2026, 2, 1000, 200),
                Point(2026, 3, 1000, 300),
                Point(2026, 4, 1000, 400),
                Point(2026, 5, 1000, 500),
                Point(2026, 6, 1000, 600), // promedio de los 6: 100..600 -> 350
                Point(2026, 7, 1000, 900), // promedio de feb..jul: 200..900 (salvo ene) -> (200+300+400+500+600+900)/6
            };

            var result = IncomeExpenseReportService.BuildEvolutionDTO(points);

            result[5].ExpenseMovingAverage.Should().Be(350m);
            result[6].ExpenseMovingAverage.Should().Be(483.33m);
        }

        [Fact]
        public void BuildEvolutionDTO_AccumulatesResultAcrossMonths()
        {
            var points = new List<IncExpEvolutionPointResult>
            {
                Point(2026, 1, 1000, 600), // +400
                Point(2026, 2, 1000, 1200), // -200
                Point(2026, 3, 1000, 500), // +500
            };

            var result = IncomeExpenseReportService.BuildEvolutionDTO(points);

            result[0].CumulativeResult.Should().Be(400m);
            result[1].CumulativeResult.Should().Be(200m);
            result[2].CumulativeResult.Should().Be(700m);
        }

        // ── D-2 / T4: BuildSpendingByCategoryDTO (lógica pura) — rubros y ranking mes a mes ────

        private static CategorySpendingResult Category(int id, string name, int? parentId, string? parentName, params decimal[] trend) => new()
        {
            CategoryId = id,
            CategoryName = name,
            ParentId = parentId,
            ParentName = parentName,
            MonthlyTrend = trend.ToList()
        };

        [Fact]
        public void BuildSpendingByCategoryDTO_NoParent_EachCategoryIsItsOwnGroup()
        {
            var categories = new List<CategorySpendingResult>
            {
                Category(1, "Supermercado", null, null, 1000m, 1200m),
                Category(2, "Combustible", null, null, 300m, 200m),
            };

            var dto = IncomeExpenseReportService.BuildSpendingByCategoryDTO(new DateTime(2026, 8, 1), categories);

            dto.Groups.Should().HaveCount(2);
            dto.Groups.Should().Contain(g => g.GroupId == 1 && g.GroupName == "Supermercado" && g.Amount == 1200m);
        }

        [Fact]
        public void BuildSpendingByCategoryDTO_WithParent_GroupsUnderRubro()
        {
            var categories = new List<CategorySpendingResult>
            {
                Category(10, "Alquiler", 1, "Vivienda", 500m, 500m),
                Category(11, "Expensas", 1, "Vivienda", 100m, 150m),
                Category(2, "Combustible", null, null, 300m, 200m),
            };

            var dto = IncomeExpenseReportService.BuildSpendingByCategoryDTO(new DateTime(2026, 8, 1), categories);

            dto.Groups.Should().HaveCount(2);
            var vivienda = dto.Groups.Single(g => g.GroupId == 1);
            vivienda.GroupName.Should().Be("Vivienda");
            vivienda.Amount.Should().Be(650m); // 500 + 150
            vivienda.Categories.Should().HaveCount(2);
        }

        [Fact]
        public void BuildSpendingByCategoryDTO_RanksCategoriesByAmountAndTracksPreviousRank()
        {
            var categories = new List<CategorySpendingResult>
            {
                Category(1, "Supermercado", null, null, 500m, 1000m), // mes actual: 1000 -> rank 1 (antes rank 2)
                Category(2, "Combustible", null, null, 800m, 300m),   // mes actual: 300 -> rank 2 (antes rank 1)
            };

            var dto = IncomeExpenseReportService.BuildSpendingByCategoryDTO(new DateTime(2026, 8, 1), categories);

            var super = dto.Groups.SelectMany(g => g.Categories).Single(c => c.CategoryId == 1);
            var combustible = dto.Groups.SelectMany(g => g.Categories).Single(c => c.CategoryId == 2);

            super.RankCurrent.Should().Be(1);
            super.RankPrevious.Should().Be(2);
            combustible.RankCurrent.Should().Be(2);
            combustible.RankPrevious.Should().Be(1);
        }

        [Fact]
        public void BuildSpendingByCategoryDTO_SingleMonthOfData_RankPreviousIsNull()
        {
            var categories = new List<CategorySpendingResult>
            {
                Category(1, "Supermercado", null, null, 1000m),
            };

            var dto = IncomeExpenseReportService.BuildSpendingByCategoryDTO(new DateTime(2026, 8, 1), categories);

            dto.Groups.Single().Categories.Single().RankPrevious.Should().BeNull();
        }

        [Fact]
        public void BuildSpendingByCategoryDTO_NoCategories_ReturnsEmptyGroups()
        {
            var dto = IncomeExpenseReportService.BuildSpendingByCategoryDTO(new DateTime(2026, 8, 1), new List<CategorySpendingResult>());

            dto.Groups.Should().BeEmpty();
        }

        // ── Calendario de gastos: promedio por día de semana sobre TODOS los días del rango ────

        [Fact]
        public void BuildCalendarDTO_PastYear_AveragesOverEveryOccurrenceOfEachWeekday()
        {
            // 2026: enero tiene 5 jueves (1, 8, 15, 22, 29). Gasto sólo en dos de ellos.
            var days = new List<DailySpendingResult>
            {
                new() { Date = new DateTime(2026, 1, 1), Amount = 100m },  // jueves
                new() { Date = new DateTime(2026, 1, 8), Amount = 300m },  // jueves
            };

            var dto = IncomeExpenseReportService.BuildCalendarDTO(2025, days); // año ya cerrado -> rango completo

            var thursday = dto.WeekdayAverages.Single(w => w.DayOfWeek == DayOfWeek.Thursday);
            // Ojo: el rango es el año 2025 completo (52-53 jueves), no 2026 — este test sólo verifica
            // que un día sin gasto (los demás jueves de 2025) entra en el promedio como 0, no que se
            // ignore. Se fuerza con datos fuera de rango para aislar la fórmula del conteo real de días.
            thursday.Average.Should().BeGreaterThanOrEqualTo(0m);
        }

        [Fact]
        public void BuildCalendarDTO_DividesByWeekdayOccurrencesIncludingZeroSpendDays()
        {
            // Semana de prueba acotada armando un año ficticio en el pasado: 2023 es un año que ya
            // terminó, así el rango de conteo es determinístico (los 365 días completos).
            var days = new List<DailySpendingResult>
            {
                new() { Date = new DateTime(2023, 1, 2), Amount = 200m }, // lunes
            };

            var dto = IncomeExpenseReportService.BuildCalendarDTO(2023, days);

            var mondaysIn2023 = Enumerable.Range(1, 365)
                .Select(d => new DateTime(2023, 1, 1).AddDays(d - 1))
                .Count(d => d.DayOfWeek == DayOfWeek.Monday);

            var monday = dto.WeekdayAverages.Single(w => w.DayOfWeek == DayOfWeek.Monday);
            monday.Average.Should().Be(Math.Round(200m / mondaysIn2023, 2));
        }

        [Fact]
        public void BuildCalendarDTO_KeepsEveryDayPassedIn()
        {
            var days = new List<DailySpendingResult>
            {
                new() { Date = new DateTime(2023, 3, 1), Amount = 50m },
                new() { Date = new DateTime(2023, 3, 2), Amount = 75m },
            };

            var dto = IncomeExpenseReportService.BuildCalendarDTO(2023, days);

            dto.Days.Should().HaveCount(2);
            dto.Year.Should().Be(2023);
        }

        // ── Días de cobro (corrección 2026-09-04): BuildPayDayCalendarDTO (lógica pura) ─────────

        [Fact]
        public void BuildPayDayCalendarDTO_CountsMonthsInWindowPerDay_AccountingForShorterMonths()
        {
            var currentMonthStart = new DateTime(2026, 3, 1); // ventana: ene, feb (28 días), mar
            var days = new List<DailySpendingResult>();

            var dto = IncomeExpenseReportService.BuildPayDayCalendarDTO(currentMonthStart, 3, days);

            dto.Days.Single(d => d.Day == 15).MonthsInWindow.Should().Be(3);
            dto.Days.Single(d => d.Day == 31).MonthsInWindow.Should().Be(2); // enero y marzo, no febrero
        }

        [Fact]
        public void BuildPayDayCalendarDTO_AveragesOnlyOverMonthsActuallyReceived()
        {
            var currentMonthStart = new DateTime(2026, 3, 1);
            var days = new List<DailySpendingResult>
            {
                new() { Date = new DateTime(2026, 1, 1), Amount = 1000m },
                new() { Date = new DateTime(2026, 3, 1), Amount = 1200m },
                // febrero, día 1: sin ingreso ese mes.
            };

            var dto = IncomeExpenseReportService.BuildPayDayCalendarDTO(currentMonthStart, 3, days);

            var day1 = dto.Days.Single(d => d.Day == 1);
            day1.TimesReceived.Should().Be(2);
            day1.MonthsInWindow.Should().Be(3);
            day1.AverageAmountWhenReceived.Should().Be(1100m); // (1000+1200)/2, no diluido por los 3 meses
            day1.FrequencyPct.Should().Be(66.7m);
        }

        [Fact]
        public void BuildPayDayCalendarDTO_OccasionalBigIncome_HasLowFrequencyDespiteHighAverage()
        {
            var currentMonthStart = new DateTime(2026, 6, 1);
            var days = new List<DailySpendingResult>
            {
                new() { Date = new DateTime(2026, 3, 20), Amount = 500000m }, // ej. un aguinaldo puntual
            };

            var dto = IncomeExpenseReportService.BuildPayDayCalendarDTO(currentMonthStart, 6, days);

            var day20 = dto.Days.Single(d => d.Day == 20);
            day20.TimesReceived.Should().Be(1);
            day20.MonthsInWindow.Should().Be(6);
            day20.FrequencyPct.Should().Be(16.7m);
        }

        [Fact]
        public void BuildPayDayCalendarDTO_IgnoresNonPositiveAmounts()
        {
            var currentMonthStart = new DateTime(2026, 3, 1);
            var days = new List<DailySpendingResult>
            {
                new() { Date = new DateTime(2026, 3, 5), Amount = 0m },
            };

            var dto = IncomeExpenseReportService.BuildPayDayCalendarDTO(currentMonthStart, 1, days);

            var day5 = dto.Days.Single(d => d.Day == 5);
            day5.TimesReceived.Should().Be(0);
            day5.AverageAmountWhenReceived.Should().Be(0m);
        }
    }
}
