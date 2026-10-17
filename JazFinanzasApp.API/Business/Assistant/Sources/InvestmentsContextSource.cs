using JazFinanzasApp.API.Business.DTO.InvestmentReport;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Inversiones (plan-devoluciones-llm.md, D2). Todo sale de IInvestmentReportService.
    //
    // Cierre de mes: el panorama de la cartera tal como está hoy (solo si el mes es el actual o el
    // anterior), cuánto del cambio de valor del mes fue mercado y cuánto aportes o retiros, y los cobros
    // de bonos del mes siguiente. Mitad de mes y revisión semanal: solo los cobros de bonos de lo que
    // queda del mes.
    public class InvestmentsContextSource : IAssistantContextSource
    {
        public const string SourceKey = "investments";

        private const int MaxHoldings = 8;

        private readonly IInvestmentReportService _reports;
        private readonly IAssistantCurrencyResolver _currencies;

        public InvestmentsContextSource(IInvestmentReportService reports, IAssistantCurrencyResolver currencies)
        {
            _reports = reports;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Inversiones (acciones, cripto y bonos, sin efectivo), en dólares y en pesos por separado. 'portfolioToday' es la cartera tal como está hoy, " +
            "con la ganancia o pérdida sobre lo invertido; en un bono suma lo ya cobrado de amortizaciones e intereses. 'monthFlow' separa el cambio de valor " +
            "del mes en lo que aportaste, lo que retiraste (los cobros de bonos cuentan como retiros) y el resultado del mercado, que es lo que queda. " +
            "En pesos, el resultado del mercado incluye el efecto del tipo de cambio. 'upcomingBondPayments' son cobros estimados, en la moneda en la " +
            "que paga cada bono, sin convertir; los indexados no tienen monto estimado.";

        public string PrimaryCurrency => "USD";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            var currencies = await _currencies.GetAsync();

            var payments = SelectBondPayments(
                await _reports.GetBondUpcomingAsync(request.UserId),
                request.IsMonthClose ? request.Month.AddMonths(1) : request.CutoffDate.Date.AddDays(1),
                request.IsMonthClose ? request.Month.AddMonths(2) : request.Month.AddMonths(1));

            if (!request.IsMonthClose)
                return payments.Count == 0 ? null : new MidMonthInvestmentsData(payments);

            var ars = await _reports.GetOverviewAsync(request.UserId, currencies.Ars.Id);
            var usd = await _reports.GetOverviewAsync(request.UserId, currencies.Usd.Id);

            var arsFlow = BuildMonthFlow(ars, request.Month, isPesos: true);
            var usdFlow = BuildMonthFlow(usd, request.Month, isPesos: false);
            var flow = arsFlow != null && usdFlow != null ? new PerCurrency<MonthFlow>(arsFlow, usdFlow) : null;

            var portfolio = request.IsLive ? BuildPortfolio(ars, usd, MaxHoldings) : null;

            if (portfolio == null && flow == null && payments.Count == 0)
                return null;

            return new MonthCloseInvestmentsData(portfolio, flow, payments);
        }

        // ── Pura: testeable sin mocks ─────────────────────────────────────────────────────────

        // Qué parte del cambio de valor del mes es del mercado: valor al cierre menos valor al cierre del mes
        // anterior, menos lo aportado más lo retirado (misma fórmula que Aportes vs rendimiento). Los cobros de
        // bonos ya están dentro de los retiros. Null si la serie no tiene alguno de los dos cierres.
        public static MonthFlow? BuildMonthFlow(InvestmentOverviewDTO overview, DateTime month, bool isPesos)
        {
            var end = overview.ValueSeries.FirstOrDefault(p => SameMonth(p.Month, month));
            var previous = overview.ValueSeries.FirstOrDefault(p => SameMonth(p.Month, month.AddMonths(-1)));
            if (end == null || previous == null) return null;

            var contributed = overview.ContributionMarkers.Where(m => SameMonth(m.Month, month)).Sum(m => m.Contributed);
            var withdrawn = overview.ContributionMarkers.Where(m => SameMonth(m.Month, month)).Sum(m => m.Withdrawn);
            var marketResult = end.Value - previous.Value - contributed + withdrawn;

            return new MonthFlow(
                AssistantFigures.RoundMoney(end.Value, isPesos),
                AssistantFigures.RoundMoney(previous.Value, isPesos),
                AssistantFigures.RoundMoney(contributed, isPesos),
                AssistantFigures.RoundMoney(withdrawn, isPesos),
                AssistantFigures.RoundMoney(marketResult, isPesos),
                AssistantFigures.Pct(marketResult + previous.Value, previous.Value));
        }

        // La cartera de hoy: totales, por tipo (bolsa, cripto, bonos) y las `maxHoldings` posiciones más
        // grandes en dólares con el resto agrupado. Los dos reportes traen las mismas posiciones.
        public static PortfolioToday BuildPortfolio(InvestmentOverviewDTO ars, InvestmentOverviewDTO usd, int maxHoldings)
        {
            var totals = new PerCurrency<PortfolioTotals>(Totals(ars, isPesos: true), Totals(usd, isPesos: false));

            var arsHoldings = ars.Holdings.Where(IsOpen).ToDictionary(h => h.AssetId);
            var usdHoldings = usd.Holdings.Where(IsOpen).ToDictionary(h => h.AssetId);
            var totalUsd = usdHoldings.Values.Sum(h => h.ActualValue);

            HoldingFigures Figures(InvestmentHoldingDTO? h, bool isPesos) => h == null
                ? new HoldingFigures(0m, 0m, 0m, null)
                : new HoldingFigures(
                    AssistantFigures.RoundMoney(h.ActualValue, isPesos),
                    AssistantFigures.RoundMoney(h.OriginalValue, isPesos),
                    AssistantFigures.RoundMoney(h.CollectedValue, isPesos),
                    h.GainLossPercent.HasValue ? AssistantFigures.RoundPct(h.GainLossPercent.Value) : null);

            var rows = arsHoldings.Keys.Union(usdHoldings.Keys)
                .Select(id =>
                {
                    arsHoldings.TryGetValue(id, out var a);
                    usdHoldings.TryGetValue(id, out var u);
                    return (Reference: (u ?? a)!, Ars: a, Usd: u);
                })
                .OrderByDescending(r => r.Usd?.ActualValue ?? 0m)
                .ThenBy(r => r.Reference.Symbol)
                .ToList();

            var top = rows.Take(maxHoldings)
                .Select(r => new HoldingEntry(
                    r.Reference.Symbol,
                    r.Reference.Bucket,
                    totalUsd > 0m ? AssistantFigures.RoundPct((r.Usd?.ActualValue ?? 0m) / totalUsd * 100m) : null,
                    new PerCurrency<HoldingFigures>(Figures(r.Ars, true), Figures(r.Usd, false))))
                .ToList();

            var rest = rows.Skip(maxHoldings).ToList();
            HoldingsOthers? others = rest.Count == 0
                ? null
                : new HoldingsOthers(
                    rest.Count,
                    new PerCurrency<decimal>(
                        AssistantFigures.RoundMoney(rest.Sum(r => r.Ars?.ActualValue ?? 0m), true),
                        AssistantFigures.RoundMoney(rest.Sum(r => r.Usd?.ActualValue ?? 0m), false)));

            var buckets = arsHoldings.Values.Select(h => h.Bucket).Union(usdHoldings.Values.Select(h => h.Bucket))
                .Distinct()
                .Select(bucket => new BucketEntry(
                    bucket,
                    totalUsd > 0m ? AssistantFigures.RoundPct(usdHoldings.Values.Where(h => h.Bucket == bucket).Sum(h => h.ActualValue) / totalUsd * 100m) : null,
                    new PerCurrency<BucketFigures>(
                        BucketOf(arsHoldings.Values.Where(h => h.Bucket == bucket), true),
                        BucketOf(usdHoldings.Values.Where(h => h.Bucket == bucket), false))))
                .OrderByDescending(b => b.Figures.Usd.Actual)
                .ThenBy(b => b.Bucket)
                .ToList();

            return new PortfolioToday(totals, buckets, top, others);
        }

        // Los cobros estimados del rango [fromInclusive, toExclusive), en la moneda de cada bono.
        public static List<BondPaymentEntry> SelectBondPayments(BondUpcomingReportDTO report, DateTime fromInclusive, DateTime toExclusive) =>
            report.Payments
                .Where(p => p.PaymentDate >= fromInclusive && p.PaymentDate < toExclusive)
                .OrderBy(p => p.PaymentDate).ThenBy(p => p.Symbol)
                .Select(p =>
                {
                    var isPesos = p.CurrencySymbol == "ARS";
                    decimal? Money(decimal? v) => v.HasValue ? AssistantFigures.RoundMoney(v.Value, isPesos) : null;
                    return new BondPaymentEntry(p.PaymentDate.ToString("yyyy-MM-dd"), p.Symbol, p.CurrencySymbol, Money(p.EstimatedCapital), Money(p.EstimatedInterest), Money(p.EstimatedTotal), p.IsIndexed);
                })
                .ToList();

        private static bool SameMonth(DateTime a, DateTime b) => a.Year == b.Year && a.Month == b.Month;

        private static bool IsOpen(InvestmentHoldingDTO h) => h.ActualValue != 0m || h.CollectedValue != 0m;

        private static PortfolioTotals Totals(InvestmentOverviewDTO o, bool isPesos) => new(
            AssistantFigures.RoundMoney(o.TotalOriginalValue, isPesos),
            AssistantFigures.RoundMoney(o.TotalActualValue, isPesos),
            AssistantFigures.RoundMoney(o.TotalCollectedValue, isPesos),
            o.GainLossPercent.HasValue ? AssistantFigures.RoundPct(o.GainLossPercent.Value) : null);

        private static BucketFigures BucketOf(IEnumerable<InvestmentHoldingDTO> holdings, bool isPesos)
        {
            var list = holdings.ToList();
            var original = list.Sum(h => h.OriginalValue);
            var actual = list.Sum(h => h.ActualValue);
            var collected = list.Sum(h => h.CollectedValue);
            var gain = InvestmentReportService.GainLossPercent(original, actual, collected);
            return new BucketFigures(
                AssistantFigures.RoundMoney(actual, isPesos),
                AssistantFigures.RoundMoney(original, isPesos),
                AssistantFigures.RoundMoney(collected, isPesos),
                gain.HasValue ? AssistantFigures.RoundPct(gain.Value) : null);
        }

        // ── Tipos de la fuente ────────────────────────────────────────────────────────────────

        public record MonthFlow(decimal ValueAtMonthEnd, decimal ValueAtPreviousMonthEnd, decimal Contributed, decimal Withdrawn, decimal MarketResult, decimal? MarketResultPct);

        public record PortfolioTotals(decimal Original, decimal Actual, decimal Collected, decimal? GainLossPct);

        public record HoldingFigures(decimal Actual, decimal Original, decimal Collected, decimal? GainLossPct);

        public record HoldingEntry(string Symbol, string Bucket, decimal? ShareOfPortfolioPct, PerCurrency<HoldingFigures> Figures);

        public record HoldingsOthers(int HoldingsCount, PerCurrency<decimal> Actual);

        public record BucketFigures(decimal Actual, decimal Original, decimal Collected, decimal? GainLossPct);

        public record BucketEntry(string Bucket, decimal? ShareOfPortfolioPct, PerCurrency<BucketFigures> Figures);

        public record PortfolioToday(PerCurrency<PortfolioTotals> Totals, IReadOnlyList<BucketEntry> Buckets, IReadOnlyList<HoldingEntry> TopHoldings, HoldingsOthers? Others);

        public record BondPaymentEntry(string Date, string Symbol, string Currency, decimal? Capital, decimal? Interest, decimal? Total, bool IsIndexed);

        public record MonthCloseInvestmentsData(PortfolioToday? PortfolioToday, PerCurrency<MonthFlow>? MonthFlow, IReadOnlyList<BondPaymentEntry> UpcomingBondPayments);

        public record MidMonthInvestmentsData(IReadOnlyList<BondPaymentEntry> UpcomingBondPayments);
    }
}
