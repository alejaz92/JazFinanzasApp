using JazFinanzasApp.API.Business.DTO.CardReport;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Tarjetas (plan-devoluciones-llm.md, D2 y D3). Mide el CONSUMO, en el mes de la compra: lo que se
    // compró, no lo que se pagó. Es una medición distinta de la de income-expense (que cuenta la cuota
    // cuando se paga) y por eso no se suman entre sí.
    //
    // Cierre de mes: consumo del mes por tarjeta contra el mes anterior y el promedio, y el compromiso
    // futuro de los 3 meses siguientes separando cuotas y recurrentes. Mitad de mes y revisión semanal:
    // consumo del mes en curso hasta hoy, y los vencimientos de lo que queda del mes.
    public class CardsContextSource : IAssistantContextSource
    {
        public const string SourceKey = "cards";

        private const int FutureMonths = 3;

        private readonly ICardReportService _cardReports;
        private readonly ICardService _cardService;
        private readonly IAssistantCurrencyResolver _currencies;

        public CardsContextSource(ICardReportService cardReports, ICardService cardService, IAssistantCurrencyResolver currencies)
        {
            _cardReports = cardReports;
            _cardService = cardService;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Consumo de tarjeta en el mes de la compra: lo que se compró, no lo que se pagó. Un recurrente cuenta en cada mes que estuvo vigente. " +
            "No se suma con income-expense, que cuenta las cuotas cuando se pagan. El cierre de mes compara cada tarjeta contra el mes anterior y el " +
            "promedio de los 6 anteriores (los meses sin consumo cuentan como cero) y trae el compromiso futuro de los 3 meses siguientes, separando " +
            "cuotas de compras y recurrentes. En la mitad de mes el consumo es del mes en curso hasta el día de corte y el mes anterior y el promedio " +
            "son meses completos: no son comparables entre sí. Los vencimientos van en pesos.";

        public string PrimaryCurrency => "ARS";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            var currencies = await _currencies.GetAsync();

            var arsSeries = ExtractSeries((await _cardReports.GetGeneralAsync(request.UserId, currencies.Ars.Id)).MonthlySeries);
            var usdSeries = ExtractSeries((await _cardReports.GetGeneralAsync(request.UserId, currencies.Usd.Id)).MonthlySeries);

            var cards = BuildCardConsumption(arsSeries, usdSeries, request.Month, request.IsMonthClose);
            if (cards.ByCard.Count == 0 && !request.IsMonthClose)
                return null;

            if (request.IsMonthClose)
            {
                var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                // El compromiso futuro se calcula con las cuotas vivas de hoy: para un cierre viejo (por
                // ejemplo una regeneración de hace varios meses) hablaría del futuro de hoy, no del de
                // ese mes, así que solo viaja cuando el mes cerrado es el anterior al actual o el actual.
                IReadOnlyList<FutureMonth> future = request.Month.AddMonths(1) >= currentMonth
                    ? await BuildFutureCommitmentAsync(request, currencies)
                    : Array.Empty<FutureMonth>();

                return new MonthCloseCardsData(cards.ByCard, cards.Total, future);
            }

            return new MidMonthCardsData(
                cards.ByCard,
                cards.Total,
                await BuildDueSoonAsync(request));
        }

        private async Task<IReadOnlyList<FutureMonth>> BuildFutureCommitmentAsync(AssistantContextRequest request, AssistantCurrencySet currencies)
        {
            var arsAll = await _cardReports.GetFutureCommitmentAsync(request.UserId, currencies.Ars.Id, includeRecurring: true);
            var arsInstallments = await _cardReports.GetFutureCommitmentAsync(request.UserId, currencies.Ars.Id, includeRecurring: false);
            var usdAll = await _cardReports.GetFutureCommitmentAsync(request.UserId, currencies.Usd.Id, includeRecurring: true);
            var usdInstallments = await _cardReports.GetFutureCommitmentAsync(request.UserId, currencies.Usd.Id, includeRecurring: false);

            return BuildFutureMonths(arsAll, arsInstallments, usdAll, usdInstallments, request.Month, FutureMonths);
        }

        // Vencimientos de lo que queda del mes: las tarjetas cuyo próximo vencimiento cae después del día
        // de corte y hasta fin de mes, con lo que hay que pagar (suma del resumen del mes, en pesos).
        private async Task<IReadOnlyList<DueEntry>> BuildDueSoonAsync(AssistantContextRequest request)
        {
            var monthEnd = request.Month.AddMonths(1).AddDays(-1);
            var cutoff = request.CutoffDate.Date;

            var due = new List<DueEntry>();
            foreach (var card in await _cardService.GetAllForUserAsync(request.UserId))
            {
                if (!card.NextDueDate.HasValue) continue;
                var dueDate = card.NextDueDate.Value.Date;
                if (dueDate <= cutoff || dueDate > monthEnd) continue;

                var summary = await _cardReports.GetMonthSummaryAsync(request.UserId, request.Month, card.Id);
                due.Add(new DueEntry(card.Name, dueDate.ToString("yyyy-MM-dd"), AssistantFigures.RoundMoney(summary.Sum(s => s.ValueInPesos), isPesos: true)));
            }

            return due.OrderBy(d => d.DueDate).ToList();
        }

        // ── Pura: testeable sin mocks ─────────────────────────────────────────────────────────

        // Consumo por tarjeta y por mes, ya expresado en la moneda de referencia del reporte: pesos más
        // dólares consumidos, convertidos a esa moneda con la cotización de cada mes.
        public static Dictionary<int, CardSeries> ExtractSeries(IEnumerable<CardMonthlySeriesPointDTO> series)
        {
            var result = new Dictionary<int, CardSeries>();
            foreach (var point in series)
            {
                var month = new DateTime(point.Month.Year, point.Month.Month, 1);
                foreach (var card in point.Cards)
                {
                    if (!result.TryGetValue(card.CardId, out var s))
                        result[card.CardId] = s = new CardSeries(card.CardName, new Dictionary<DateTime, decimal>());
                    s.ByMonth[month] = s.ByMonth.GetValueOrDefault(month) + card.PesosAmount + card.DollarsAmount;
                }
            }
            return result;
        }

        // Consumo por tarjeta del mes pedido. En un cierre, contra el mes anterior y el promedio de los
        // 6 anteriores; en la mitad de mes, el consumo parcial junto a los meses completos de referencia.
        public static CardConsumption BuildCardConsumption(
            IReadOnlyDictionary<int, CardSeries> ars, IReadOnlyDictionary<int, CardSeries> usd, DateTime month, bool isMonthClose)
        {
            decimal At(IReadOnlyDictionary<int, CardSeries> source, int cardId, DateTime m) =>
                source.TryGetValue(cardId, out var s) ? s.ByMonth.GetValueOrDefault(m) : 0m;

            decimal Average(IReadOnlyDictionary<int, CardSeries> source, int cardId)
            {
                var sum = 0m;
                for (int i = 1; i <= IncomeExpenseReportService.ToDateComparisonMonths; i++)
                    sum += At(source, cardId, month.AddMonths(-i));
                return sum / IncomeExpenseReportService.ToDateComparisonMonths;
            }

            object Figures(IReadOnlyDictionary<int, CardSeries> source, int cardId, bool isPesos)
            {
                var current = At(source, cardId, month);
                var previous = At(source, cardId, month.AddMonths(-1));
                var average = Average(source, cardId);
                return isMonthClose
                    ? AssistantFigures.Compare(current, previous, average, isPesos)
                    : new PartialConsumption(
                        AssistantFigures.RoundMoney(current, isPesos),
                        AssistantFigures.RoundMoney(previous, isPesos),
                        AssistantFigures.RoundMoney(average, isPesos));
            }

            var cardIds = ars.Keys.Union(usd.Keys).ToList();
            var byCard = cardIds
                .Select(id => new
                {
                    Id = id,
                    Name = (ars.TryGetValue(id, out var a) ? a : usd[id]).Name,
                    ArsCurrent = At(ars, id, month),
                    ArsAverage = Average(ars, id),
                    ArsPrevious = At(ars, id, month.AddMonths(-1)),
                    UsdCurrent = At(usd, id, month),
                    UsdAverage = Average(usd, id),
                    UsdPrevious = At(usd, id, month.AddMonths(-1))
                })
                .Where(c => c.ArsCurrent != 0m || c.ArsPrevious != 0m || c.ArsAverage != 0m
                         || c.UsdCurrent != 0m || c.UsdPrevious != 0m || c.UsdAverage != 0m)
                .OrderByDescending(c => Math.Max(c.ArsCurrent, c.ArsAverage))
                .ThenBy(c => c.Name)
                .Select(c => new CardEntry(c.Name, new PerCurrency<object>(Figures(ars, c.Id, true), Figures(usd, c.Id, false))))
                .ToList();

            object Total(IReadOnlyDictionary<int, CardSeries> source, bool isPesos)
            {
                var current = source.Keys.Sum(id => At(source, id, month));
                var previous = source.Keys.Sum(id => At(source, id, month.AddMonths(-1)));
                var average = source.Keys.Sum(id => Average(source, id));
                return isMonthClose
                    ? AssistantFigures.Compare(current, previous, average, isPesos)
                    : new PartialConsumption(
                        AssistantFigures.RoundMoney(current, isPesos),
                        AssistantFigures.RoundMoney(previous, isPesos),
                        AssistantFigures.RoundMoney(average, isPesos));
            }

            return new CardConsumption(byCard, new PerCurrency<object>(Total(ars, true), Total(usd, false)));
        }

        // Cuotas y recurrentes de los `months` meses siguientes a `month`. Los recurrentes son lo que
        // queda al restar las cuotas del total, porque el reporte solo permite pedir "con" o "sin" ellos.
        public static List<FutureMonth> BuildFutureMonths(
            CardFutureCommitmentDTO arsAll, CardFutureCommitmentDTO arsInstallments,
            CardFutureCommitmentDTO usdAll, CardFutureCommitmentDTO usdInstallments,
            DateTime month, int months)
        {
            decimal Sum(CardFutureCommitmentDTO report, DateTime m) =>
                report.MonthlySeries
                    .Where(s => s.Month.Year == m.Year && s.Month.Month == m.Month)
                    .Sum(s => s.Purchases.Sum(p => p.Amount));

            var result = new List<FutureMonth>();
            for (int i = 1; i <= months; i++)
            {
                var m = month.AddMonths(i);
                var arsTotal = Sum(arsAll, m);
                var arsInst = Sum(arsInstallments, m);
                var usdTotal = Sum(usdAll, m);
                var usdInst = Sum(usdInstallments, m);

                if (arsTotal == 0m && usdTotal == 0m) continue;

                result.Add(new FutureMonth(
                    m.ToString("yyyy-MM"),
                    new PerCurrency<decimal>(AssistantFigures.RoundMoney(arsInst, true), AssistantFigures.RoundMoney(usdInst, false)),
                    new PerCurrency<decimal>(AssistantFigures.RoundMoney(arsTotal - arsInst, true), AssistantFigures.RoundMoney(usdTotal - usdInst, false))));
            }
            return result;
        }

        // ── Tipos de la fuente ────────────────────────────────────────────────────────────────

        public record CardSeries(string Name, Dictionary<DateTime, decimal> ByMonth);

        public record PartialConsumption(decimal CurrentToDate, decimal PreviousMonthFull, decimal AverageMonthFull);

        public record CardEntry(string Card, PerCurrency<object> Consumption);

        public record CardConsumption(IReadOnlyList<CardEntry> ByCard, PerCurrency<object> Total);

        public record FutureMonth(string Month, PerCurrency<decimal> Installments, PerCurrency<decimal> Recurring);

        public record DueEntry(string Card, string DueDate, decimal AmountPesos);

        public record MonthCloseCardsData(IReadOnlyList<CardEntry> ByCard, PerCurrency<object> Total, IReadOnlyList<FutureMonth> FutureCommitment);

        public record MidMonthCardsData(IReadOnlyList<CardEntry> ByCard, PerCurrency<object> Total, IReadOnlyList<DueEntry> DueSoon);
    }
}
