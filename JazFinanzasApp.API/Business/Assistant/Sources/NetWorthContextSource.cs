using JazFinanzasApp.API.Business.DTO.NetWorth;
using JazFinanzasApp.API.Business.Interfaces;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Patrimonio (plan-devoluciones-llm.md, D2): el patrimonio a fin de mes contra el fin del mes
    // anterior, en las dos monedas. Todo sale de INetWorthReportService.GetMonthlySeriesAsync, el mismo
    // cálculo de Patrimonio → General.
    //
    // Cierre de mes: el total, su composición (cuentas, acciones, cripto y bonos) y la serie de los
    // últimos 6 meses. Mitad de mes y revisión semanal: solo el último valor y su variación desde el
    // fin del mes anterior.
    public class NetWorthContextSource : IAssistantContextSource
    {
        public const string SourceKey = "net-worth";

        private const int SeriesMonths = 6;

        private readonly INetWorthReportService _netWorth;
        private readonly IAssistantCurrencyResolver _currencies;

        public NetWorthContextSource(INetWorthReportService netWorth, IAssistantCurrencyResolver currencies)
        {
            _netWorth = netWorth;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Patrimonio total valuado a fin de mes (cuentas, acciones, cripto y bonos), en dólares y en pesos por separado, comparado con el fin del " +
            "mes anterior. En la mitad de mes el último valor es el de hoy. La variación mezcla aportes, retiros y el resultado del mercado: para " +
            "separarlos está la fuente investments. En pesos, el patrimonio también se mueve con el tipo de cambio.";

        public string PrimaryCurrency => "USD";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            var currencies = await _currencies.GetAsync();
            var ars = IndexByMonth(await _netWorth.GetMonthlySeriesAsync(request.UserId, currencies.Ars.Id));
            var usd = IndexByMonth(await _netWorth.GetMonthlySeriesAsync(request.UserId, currencies.Usd.Id));

            if (!ars.ContainsKey(request.Month) && !usd.ContainsKey(request.Month))
                return null;

            var total = new PerCurrency<ValueChange>(
                TotalChange(ars, request.Month, isPesos: true),
                TotalChange(usd, request.Month, isPesos: false));

            if (!request.IsMonthClose)
                return new MidMonthNetWorthData(total);

            return new MonthCloseNetWorthData(
                total,
                new PerCurrency<Composition>(BuildComposition(ars, request.Month, isPesos: true), BuildComposition(usd, request.Month, isPesos: false)),
                BuildSeries(ars, usd, request.Month, SeriesMonths));
        }

        // ── Pura: testeable sin mocks ─────────────────────────────────────────────────────────

        public static Dictionary<DateTime, NetWorthMonthlyPointDTO> IndexByMonth(IEnumerable<NetWorthMonthlyPointDTO> points) =>
            points
                .GroupBy(p => new DateTime(p.Month.Year, p.Month.Month, 1))
                .ToDictionary(g => g.Key, g => g.First());

        // Un mes que no figura en la serie vale cero.
        private static decimal At(IReadOnlyDictionary<DateTime, NetWorthMonthlyPointDTO> points, DateTime month, Func<NetWorthMonthlyPointDTO, decimal> pick) =>
            points.TryGetValue(month, out var p) ? pick(p) : 0m;

        public static ValueChange TotalChange(IReadOnlyDictionary<DateTime, NetWorthMonthlyPointDTO> points, DateTime month, bool isPesos) =>
            AssistantFigures.Change(At(points, month, p => p.Total), At(points, month.AddMonths(-1), p => p.Total), isPesos);

        public static Composition BuildComposition(IReadOnlyDictionary<DateTime, NetWorthMonthlyPointDTO> points, DateTime month, bool isPesos)
        {
            ValueChange Of(Func<NetWorthMonthlyPointDTO, decimal> pick) =>
                AssistantFigures.Change(At(points, month, pick), At(points, month.AddMonths(-1), pick), isPesos);

            return new Composition(
                Of(p => p.Accounts),
                Of(p => p.Stocks),
                Of(p => p.CryptoStable + p.CryptoVolatile),
                Of(p => p.Bonds));
        }

        // Los totales de fin de mes de los últimos `months` meses, del más viejo al más nuevo.
        public static List<SeriesPoint> BuildSeries(
            IReadOnlyDictionary<DateTime, NetWorthMonthlyPointDTO> ars, IReadOnlyDictionary<DateTime, NetWorthMonthlyPointDTO> usd, DateTime month, int months)
        {
            var result = new List<SeriesPoint>();
            for (int i = months - 1; i >= 0; i--)
            {
                var m = month.AddMonths(-i);
                if (!ars.ContainsKey(m) && !usd.ContainsKey(m)) continue;
                result.Add(new SeriesPoint(
                    m.ToString("yyyy-MM"),
                    AssistantFigures.RoundMoney(At(ars, m, p => p.Total), isPesos: true),
                    AssistantFigures.RoundMoney(At(usd, m, p => p.Total), isPesos: false)));
            }
            return result;
        }

        public record Composition(ValueChange Accounts, ValueChange Stocks, ValueChange Crypto, ValueChange Bonds);

        public record SeriesPoint(string Month, decimal Ars, decimal Usd);

        public record MonthCloseNetWorthData(PerCurrency<ValueChange> Total, PerCurrency<Composition> Composition, IReadOnlyList<SeriesPoint> Last6Months);

        public record MidMonthNetWorthData(PerCurrency<ValueChange> Total);
    }
}
