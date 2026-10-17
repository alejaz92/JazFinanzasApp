using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Ingresos y egresos de la cuenta (plan-devoluciones-llm.md, D2 y D3). Todo sale de
    // IIncomeExpenseReportService: la fuente solo elige qué mandar y arma las comparaciones, no
    // recalcula nada que el reporte ya calcule.
    //
    // Cierre de mes: totales del mes (ingresos, egresos, resultado y tasa de ahorro) contra el mes
    // anterior y el promedio de los 6 anteriores, egresos por categoría y composición de ingresos.
    // Mitad de mes y revisión semanal: gasto del día 1 al día de corte contra el mismo tramo del mes
    // anterior y del promedio, por categoría, y la proyección de cierre del termómetro de Inicio.
    public class IncomeExpenseContextSource : IAssistantContextSource
    {
        public const string SourceKey = "income-expense";

        private const int MaxExpenseCategories = 10;
        private const int MaxIncomeCategories = 8;

        private readonly IIncomeExpenseReportService _reports;
        private readonly IAssistantCurrencyResolver _currencies;

        public IncomeExpenseContextSource(IIncomeExpenseReportService reports, IAssistantCurrencyResolver currencies)
        {
            _reports = reports;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Ingresos y egresos de las cuentas. Un gasto de tarjeta cuenta cuando se paga la cuota, es decir lo que salió de las cuentas, " +
            "no lo que se compró: no se suma con la fuente de tarjetas. Cada monto trae su variación contra el mes anterior y contra el " +
            "promedio de los 6 meses anteriores, en pesos y en dólares por separado (los meses sin movimiento cuentan como cero en el promedio). " +
            "En la mitad de mes y en la revisión semanal el gasto es del día 1 al día de corte y se compara con el mismo tramo de los otros meses; " +
            "la proyección de cierre es lineal al ritmo actual. Pesos sin decimales y dólares con dos.";

        public string PrimaryCurrency => "ARS";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            var currencies = await _currencies.GetAsync();

            var monthsFromToday = Math.Max(0, (DateTime.Today.Year - request.Month.Year) * 12 + DateTime.Today.Month - request.Month.Month);
            var evolutionMonths = monthsFromToday + 1 + IncomeExpenseReportService.ToDateComparisonMonths;

            var ars = await LoadAsync(request, currencies.Ars, evolutionMonths);
            var usd = await LoadAsync(request, currencies.Usd, evolutionMonths);

            var categoryCut = BuildExpenseCategoryCut(ars.ToDate, usd.ToDate, MaxExpenseCategories);

            if (request.IsMonthClose)
            {
                return new MonthCloseData(
                    new PerCurrency<MoneyTotals>(BuildTotals(ars.Window, isPesos: true), BuildTotals(usd.Window, isPesos: false)),
                    categoryCut,
                    BuildIncomeCut(ars.IncomeComposition!, usd.IncomeComposition!, MaxIncomeCategories));
            }

            return new MidMonthData(
                request.CutoffDay,
                request.DaysInMonth,
                new PerCurrency<Comparison>(
                    AssistantFigures.Compare(ars.ToDate.TotalCurrent, ars.ToDate.TotalPrevious, ars.ToDate.TotalAverage, isPesos: true),
                    AssistantFigures.Compare(usd.ToDate.TotalCurrent, usd.ToDate.TotalPrevious, usd.ToDate.TotalAverage, isPesos: false)),
                categoryCut,
                new PerCurrency<Projection>(
                    BuildProjection(ars.ProjectedMonthEnd, ars.Window, isPesos: true),
                    BuildProjection(usd.ProjectedMonthEnd, usd.Window, isPesos: false)));
        }

        private async Task<CurrencyData> LoadAsync(AssistantContextRequest request, Asset asset, int evolutionMonths)
        {
            var toDate = await _reports.GetSpendingToDateByCategoryAsync(request.UserId, asset.Id, request.Month, request.CutoffDay);
            var evolution = (await _reports.GetEvolutionAsync(request.UserId, asset.Id, evolutionMonths)).ToList();
            var window = ExtractWindow(evolution, request.Month, IncomeExpenseReportService.ToDateComparisonMonths);

            IncomeCompositionDTO? income = null;
            decimal projected = 0m;
            if (request.IsMonthClose)
                income = await _reports.GetIncomeCompositionAsync(request.UserId, request.Month, asset.Id);
            else
                projected = await GetProjectedMonthEndAsync(request, asset);

            return new CurrencyData(toDate, window, income, projected);
        }

        // Mismo cálculo que el termómetro de Inicio (DashboardService.BuildThermometer), tomando el día
        // de corte como "hoy": la proyección es la que ya se ve en la app, no una cuenta nueva.
        private async Task<decimal> GetProjectedMonthEndAsync(AssistantContextRequest request, Asset asset)
        {
            var cutoff = request.CutoffDate.Date;
            var days = (await _reports.GetCalendarAsync(request.UserId, asset.Id, cutoff.Year)).Days.ToList();

            var previousMonthYear = new DateTime(cutoff.Year, cutoff.Month, 1).AddMonths(-1).Year;
            if (previousMonthYear != cutoff.Year)
                days.AddRange((await _reports.GetCalendarAsync(request.UserId, asset.Id, previousMonthYear)).Days);

            return DashboardService.BuildThermometer(days, cutoff).ProjectedMonthEndAmount;
        }

        // ── Pura: testeable sin mocks ─────────────────────────────────────────────────────────

        // Ingresos y egresos del mes, del anterior y el promedio de los `comparisonMonths` anteriores.
        // Un mes que no figura en la serie cuenta como cero.
        public static MonthWindow ExtractWindow(IEnumerable<IncExpEvolutionPointDTO> points, DateTime month, int comparisonMonths)
        {
            var byMonth = points
                .GroupBy(p => new DateTime(p.Month.Year, p.Month.Month, 1))
                .ToDictionary(g => g.Key, g => g.First());

            MonthTotals At(DateTime m) => byMonth.TryGetValue(m, out var p) ? new MonthTotals(p.Income, p.Expense) : new MonthTotals(0m, 0m);

            var incomeSum = 0m;
            var expenseSum = 0m;
            for (int i = 1; i <= comparisonMonths; i++)
            {
                var t = At(month.AddMonths(-i));
                incomeSum += t.Income;
                expenseSum += t.Expense;
            }

            var average = comparisonMonths > 0
                ? new MonthTotals(incomeSum / comparisonMonths, expenseSum / comparisonMonths)
                : new MonthTotals(0m, 0m);

            return new MonthWindow(At(month), At(month.AddMonths(-1)), average);
        }

        public static MoneyTotals BuildTotals(MonthWindow w, bool isPesos) => new(
            AssistantFigures.Compare(w.Current.Income, w.Previous.Income, w.Average.Income, isPesos),
            AssistantFigures.Compare(w.Current.Expense, w.Previous.Expense, w.Average.Expense, isPesos),
            AssistantFigures.Compare(w.Current.Result, w.Previous.Result, w.Average.Result, isPesos),
            AssistantFigures.CompareRate(
                AssistantFigures.Rate(w.Current.Result, w.Current.Income),
                AssistantFigures.Rate(w.Previous.Result, w.Previous.Income),
                AssistantFigures.Rate(w.Average.Result, w.Average.Income)));

        public static Projection BuildProjection(decimal projectedMonthEnd, MonthWindow w, bool isPesos) => new(
            AssistantFigures.RoundMoney(projectedMonthEnd, isPesos),
            AssistantFigures.RoundMoney(w.Previous.Expense, isPesos),
            AssistantFigures.RoundMoney(w.Average.Expense, isPesos),
            AssistantFigures.Pct(projectedMonthEnd, w.Previous.Expense),
            AssistantFigures.Pct(projectedMonthEnd, w.Average.Expense));

        // Las `maxCategories` categorías de mayor gasto y el resto agrupado en "otras". Se ordenan por el
        // mayor entre lo del tramo y el promedio, en pesos: así una categoría que cayó a cero pero venía
        // pesando mucho no desaparece de la lista. Las dos monedas muestran las mismas categorías.
        public static ExpenseCategoryCut BuildExpenseCategoryCut(SpendingToDateDTO ars, SpendingToDateDTO usd, int maxCategories)
        {
            var arsById = ars.Categories.ToDictionary(c => c.CategoryId);
            var usdById = usd.Categories.ToDictionary(c => c.CategoryId);

            var rows = arsById.Keys.Union(usdById.Keys)
                .Select(id =>
                {
                    arsById.TryGetValue(id, out var a);
                    usdById.TryGetValue(id, out var u);
                    var reference = a ?? u!;
                    return new CategoryRow(
                        reference.CategoryName,
                        reference.ParentName,
                        a?.CurrentToDate ?? 0m, a?.PreviousToDate ?? 0m, a?.AverageToDate ?? 0m,
                        u?.CurrentToDate ?? 0m, u?.PreviousToDate ?? 0m, u?.AverageToDate ?? 0m);
                })
                .OrderByDescending(r => Math.Max(r.ArsCurrent, r.ArsAverage))
                .ThenBy(r => r.Name)
                .ToList();

            var entries = rows.Take(maxCategories)
                .Select(r => new CategoryEntry(
                    r.Name,
                    r.Rubro,
                    ars.TotalCurrent > 0m ? AssistantFigures.RoundPct(r.ArsCurrent / ars.TotalCurrent * 100m) : null,
                    AssistantFigures.Compare(r.ArsCurrent, r.ArsPrevious, r.ArsAverage, isPesos: true),
                    AssistantFigures.Compare(r.UsdCurrent, r.UsdPrevious, r.UsdAverage, isPesos: false)))
                .ToList();

            var rest = rows.Skip(maxCategories).ToList();
            OthersEntry? others = rest.Count == 0
                ? null
                : new OthersEntry(
                    rest.Count,
                    AssistantFigures.Compare(rest.Sum(r => r.ArsCurrent), rest.Sum(r => r.ArsPrevious), rest.Sum(r => r.ArsAverage), isPesos: true),
                    AssistantFigures.Compare(rest.Sum(r => r.UsdCurrent), rest.Sum(r => r.UsdPrevious), rest.Sum(r => r.UsdAverage), isPesos: false));

            return new ExpenseCategoryCut(entries, others);
        }

        // Composición de ingresos del mes: las `maxCategories` mayores en pesos y el resto en "otros".
        public static IncomeCut BuildIncomeCut(IncomeCompositionDTO ars, IncomeCompositionDTO usd, int maxCategories)
        {
            var arsById = ars.Categories.ToDictionary(c => c.CategoryId);
            var usdById = usd.Categories.ToDictionary(c => c.CategoryId);
            var totalArs = ars.Categories.Sum(c => c.Amount);

            var rows = arsById.Keys.Union(usdById.Keys)
                .Select(id =>
                {
                    arsById.TryGetValue(id, out var a);
                    usdById.TryGetValue(id, out var u);
                    return (Name: (a ?? u!).CategoryName, Ars: a?.Amount ?? 0m, Usd: u?.Amount ?? 0m);
                })
                .OrderByDescending(r => r.Ars)
                .ThenBy(r => r.Name)
                .ToList();

            var items = rows.Take(maxCategories)
                .Select(r => new IncomeItem(
                    r.Name,
                    AssistantFigures.RoundMoney(r.Ars, isPesos: true),
                    AssistantFigures.RoundMoney(r.Usd, isPesos: false),
                    totalArs > 0m ? AssistantFigures.RoundPct(r.Ars / totalArs * 100m) : null))
                .ToList();

            var rest = rows.Skip(maxCategories).ToList();
            IncomeOthers? others = rest.Count == 0
                ? null
                : new IncomeOthers(
                    rest.Count,
                    AssistantFigures.RoundMoney(rest.Sum(r => r.Ars), isPesos: true),
                    AssistantFigures.RoundMoney(rest.Sum(r => r.Usd), isPesos: false),
                    totalArs > 0m ? AssistantFigures.RoundPct(rest.Sum(r => r.Ars) / totalArs * 100m) : null);

            return new IncomeCut(items, others);
        }

        // ── Tipos de la fuente ────────────────────────────────────────────────────────────────

        private record CurrencyData(SpendingToDateDTO ToDate, MonthWindow Window, IncomeCompositionDTO? IncomeComposition, decimal ProjectedMonthEnd);

        private record CategoryRow(string Name, string? Rubro, decimal ArsCurrent, decimal ArsPrevious, decimal ArsAverage, decimal UsdCurrent, decimal UsdPrevious, decimal UsdAverage);

        public record MonthTotals(decimal Income, decimal Expense)
        {
            public decimal Result => Income - Expense;
        }

        public record MonthWindow(MonthTotals Current, MonthTotals Previous, MonthTotals Average);

        public record MoneyTotals(Comparison Income, Comparison Expense, Comparison Result, RateComparison SavingsRate);

        public record Projection(decimal ProjectedMonthEnd, decimal PreviousMonthTotal, decimal AverageMonthTotal, decimal? PctVsPreviousMonth, decimal? PctVsAverage);

        public record CategoryEntry(string Name, string? Rubro, decimal? ShareOfExpensePct, Comparison Ars, Comparison Usd);

        public record OthersEntry(int CategoriesCount, Comparison Ars, Comparison Usd);

        public record ExpenseCategoryCut(IReadOnlyList<CategoryEntry> Categories, OthersEntry? Others);

        public record IncomeItem(string Name, decimal Ars, decimal Usd, decimal? SharePct);

        public record IncomeOthers(int CategoriesCount, decimal Ars, decimal Usd, decimal? SharePct);

        public record IncomeCut(IReadOnlyList<IncomeItem> Items, IncomeOthers? Others);

        public record MonthCloseData(PerCurrency<MoneyTotals> Totals, ExpenseCategoryCut ExpensesByCategory, IncomeCut IncomeComposition);

        public record MidMonthData(int CutoffDay, int DaysInMonth, PerCurrency<Comparison> ExpenseToDate, ExpenseCategoryCut ExpensesByCategory, PerCurrency<Projection> Projection);
    }
}
