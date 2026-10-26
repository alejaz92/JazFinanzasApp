using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Movimientos destacados (plan-devoluciones-llm.md, D2; plan-devoluciones-llm-v2.md, D1): los egresos individuales más grandes
    // del período y, para las categorías que más subieron contra su promedio, sus movimientos principales. Es lo que permite
    // explicar por qué subió una categoría ("el service del auto") en vez de solo decir que subió. Es la única fuente que manda
    // texto libre del usuario (el detalle del movimiento), de ahí el tope de movimientos y de largo (decisión 5, "Privacidad").
    //
    // Mide igual que income-expense, por fecha de compra: un gasto de tarjeta aparece como la compra, en su fecha y por su total, no
    // como la cuota que se pagó. Comparte con esa fuente la consulta de gasto (IPurchaseSpendingService guarda las filas del pedido).
    public class TopExpensesContextSource : IAssistantContextSource
    {
        public const string SourceKey = "top-expenses";

        private const int LargestCount = 10;
        private const int RisingCategories = 3;
        private const int MovementsPerCategory = 3;

        // Mismo corte que income-expense para que las dos fuentes compartan la misma consulta.
        private const int FullMonthCutoff = 31;

        private readonly IPurchaseSpendingService _spending;
        private readonly IAssistantCurrencyResolver _currencies;

        public TopExpensesContextSource(IPurchaseSpendingService spending, IAssistantCurrencyResolver currencies)
        {
            _spending = spending;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Los egresos individuales más grandes del período, con el detalle tal como lo cargó el usuario, y los movimientos principales " +
            "de las 3 categorías que más subieron contra su promedio. Se miden por fecha de compra, igual que income-expense: una compra con tarjeta " +
            "aparece como la compra, en su fecha y por su total (una compra en cuotas, entera), no como la cuota que se pagó; 'origin' dice si salió " +
            "de una cuenta (account) o fue una compra con tarjeta (card). Los cargos recurrentes de tarjeta no figuran: no son un movimiento puntual. " +
            "Cada monto va en pesos y en dólares. Sirve para explicar por qué subió una categoría, no para sumar: los totales están en income-expense.";

        public string PrimaryCurrency => "ARS";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            var currencies = await _currencies.GetAsync();
            var assetIds = new[] { currencies.Ars.Id, currencies.Usd.Id };

            var from = request.Month;
            var toExclusive = request.Month.AddDays(request.CutoffDay);

            var largest = await _spending.GetLargestAsync(request.UserId, assetIds, from, toExclusive, LargestCount);
            if (largest.Count == 0) return null;

            var cutoff = request.IsMonthClose ? FullMonthCutoff : request.CutoffDay;
            var windows = await _spending.GetWindowsAsync(request.UserId, assetIds, request.Month, cutoff);
            var toDate = windows[currencies.Ars.Id].ToDate;

            var rising = new List<RisingCategory>();
            foreach (var category in SelectRisingCategories(toDate.Categories.Select(c => (c.CategoryId, c.CategoryName, c.CurrentToDate, c.AverageToDate)), RisingCategories))
            {
                var movements = await _spending.GetLargestAsync(request.UserId, assetIds, from, toExclusive, MovementsPerCategory, category.CategoryId);
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

        private static Movement ToMovement(PurchaseSpendingRowResult m, AssistantCurrencySet currencies) => new(
            m.Date.ToString("yyyy-MM-dd"),
            m.CategoryName,
            AssistantFigures.ShortenDetail(m.Detail),
            m.Kind == PurchaseSpendingKind.CardPurchase ? "card" : "account",
            AssistantFigures.RoundMoney(m.AmountByAssetId[currencies.Ars.Id], isPesos: true),
            AssistantFigures.RoundMoney(m.AmountByAssetId[currencies.Usd.Id], isPesos: false));

        public record Movement(string Date, string Category, string Detail, string Origin, decimal Ars, decimal Usd);

        public record RisingCategory(string Category, IReadOnlyList<Movement> Movements);

        public record TopExpensesData(string From, string To, IReadOnlyList<Movement> Largest, IReadOnlyList<RisingCategory> CategoriesThatRoseMost);
    }
}
