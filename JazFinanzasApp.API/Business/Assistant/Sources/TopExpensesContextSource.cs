using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Movimientos destacados (plan-devoluciones-llm.md, D2): los egresos individuales más grandes del
    // período y, para las categorías que más subieron contra su promedio, sus movimientos principales.
    // Es lo que permite explicar por qué subió una categoría ("el service del auto") en vez de solo
    // decir que subió. Es la única fuente que manda texto libre del usuario (el detalle del
    // movimiento), de ahí el tope de movimientos y de largo (decisión 5, "Privacidad").
    public class TopExpensesContextSource : IAssistantContextSource
    {
        public const string SourceKey = "top-expenses";

        private const int LargestCount = 10;
        private const int RisingCategories = 3;
        private const int MovementsPerCategory = 3;

        private readonly ITransactionRepository _transactionRepository;
        private readonly IIncomeExpenseReportService _reports;
        private readonly IAssistantCurrencyResolver _currencies;

        public TopExpensesContextSource(
            ITransactionRepository transactionRepository,
            IIncomeExpenseReportService reports,
            IAssistantCurrencyResolver currencies)
        {
            _transactionRepository = transactionRepository;
            _reports = reports;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Los egresos individuales más grandes del período, con el detalle tal como lo cargó el usuario, y los movimientos principales " +
            "de las 3 categorías que más subieron contra su promedio. Un gasto de tarjeta aparece como la cuota que se pagó, no como la compra. " +
            "Cada monto va en pesos y en dólares. Sirve para explicar por qué subió una categoría, no para sumar: los totales están en income-expense.";

        public string PrimaryCurrency => "ARS";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            var currencies = await _currencies.GetAsync();
            var assets = new[] { currencies.Ars, currencies.Usd };

            var from = request.Month;
            var toExclusive = request.Month.AddDays(request.CutoffDay);

            var largest = (await _transactionRepository.GetLargestExpensesAsync(request.UserId, assets, from, toExclusive, LargestCount)).ToList();
            if (largest.Count == 0) return null;

            var toDate = await _reports.GetSpendingToDateByCategoryAsync(request.UserId, currencies.Ars.Id, request.Month, request.CutoffDay);

            var rising = new List<RisingCategory>();
            foreach (var category in SelectRisingCategories(toDate.Categories.Select(c => (c.CategoryId, c.CategoryName, c.CurrentToDate, c.AverageToDate)), RisingCategories))
            {
                var movements = await _transactionRepository.GetLargestExpensesAsync(
                    request.UserId, assets, from, toExclusive, MovementsPerCategory, category.CategoryId);
                rising.Add(new RisingCategory(category.CategoryName, movements.Select(m => ToMovement(m, currencies)).ToList()));
            }

            return new TopExpensesData(
                from.ToString("yyyy-MM-dd"),
                toExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
                largest.Select(m => ToMovement(m, currencies)).ToList(),
                rising);
        }

        // Las categorías con más gasto en el tramo que su promedio, ordenadas por cuánto subieron en
        // pesos. Las que bajaron o quedaron igual no entran: no hay movimiento que explicar.
        public static List<(int CategoryId, string CategoryName)> SelectRisingCategories(
            IEnumerable<(int CategoryId, string CategoryName, decimal Current, decimal Average)> categories, int take) =>
            categories
                .Where(c => c.Current > 0m && c.Current > c.Average)
                .OrderByDescending(c => c.Current - c.Average)
                .ThenBy(c => c.CategoryName)
                .Take(take)
                .Select(c => (c.CategoryId, c.CategoryName))
                .ToList();

        private static Movement ToMovement(ExpenseMovementResult m, AssistantCurrencySet currencies) => new(
            m.Date.ToString("yyyy-MM-dd"),
            m.CategoryName,
            AssistantFigures.ShortenDetail(m.Detail),
            AssistantFigures.RoundMoney(m.AmountByAssetId[currencies.Ars.Id], isPesos: true),
            AssistantFigures.RoundMoney(m.AmountByAssetId[currencies.Usd.Id], isPesos: false));

        public record Movement(string Date, string Category, string Detail, decimal Ars, decimal Usd);

        public record RisingCategory(string Category, IReadOnlyList<Movement> Movements);

        public record TopExpensesData(string From, string To, IReadOnlyList<Movement> Largest, IReadOnlyList<RisingCategory> CategoriesThatRoseMost);
    }
}
