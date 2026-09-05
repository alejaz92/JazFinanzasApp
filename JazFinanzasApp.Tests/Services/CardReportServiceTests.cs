using FluentAssertions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 14 (Tarjetas): solo los métodos puros del servicio — mismo criterio que
    // NetWorthReportServiceTests para T8 (CountLiveInstallmentMonths), sin mocks de repositorio.
    public class CardReportServiceTests
    {
        private static CardTransaction MakeCardTransaction(
            int id, int cardId, string repeat, DateTime firstInstallment, int installments,
            decimal totalAmount = 3000m, decimal installmentAmount = 1000m,
            string assetName = "Peso Argentino", string cardName = "Visa Santander",
            string detail = "Compra", DateTime? date = null, int transactionClassId = 1, string categoryName = "Supermercado")
            => new CardTransaction
            {
                Id = id,
                CardId = cardId,
                Card = new Card { Id = cardId, Name = cardName },
                Repeat = repeat,
                FirstInstallment = firstInstallment,
                Installments = installments,
                InstallmentAmount = installmentAmount,
                TotalAmount = totalAmount,
                AssetId = 1,
                Asset = new Asset { Id = 1, Name = assetName },
                Detail = detail,
                Date = date ?? firstInstallment,
                TransactionClassId = transactionClassId,
                TransactionClass = new TransactionClass { Id = transactionClassId, Description = categoryName }
            };

        // ── BuildMonthlyConsumptionSeries: devengado, pesos y dólares nunca se mezclan ──────────

        [Fact]
        public void BuildMonthlyConsumptionSeries_SplitsByCurrencyAndCard()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var pesoCt = MakeCardTransaction(1, 10, "NO", latestMonth, 1, totalAmount: 5000m, assetName: "Peso Argentino", cardName: "Visa");
            var dollarCt = MakeCardTransaction(2, 20, "NO", latestMonth, 1, totalAmount: 100m, assetName: "Dolar Estadounidense", cardName: "Amex");

            var series = CardReportService.BuildMonthlyConsumptionSeries(new List<CardTransaction> { pesoCt, dollarCt }, latestMonth, 3);

            series.Should().HaveCount(3);
            var lastPoint = series.Last();
            lastPoint.Month.Should().Be(latestMonth);
            lastPoint.Cards.Should().HaveCount(2);
            lastPoint.Cards.Single(c => c.CardId == 10).PesosAmount.Should().Be(5000m);
            lastPoint.Cards.Single(c => c.CardId == 10).DollarsAmount.Should().Be(0m);
            lastPoint.Cards.Single(c => c.CardId == 20).DollarsAmount.Should().Be(100m);
        }

        [Fact]
        public void BuildMonthlyConsumptionSeries_MonthWithNoData_HasZeroForKnownCards()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", latestMonth, 1, totalAmount: 5000m);

            var series = CardReportService.BuildMonthlyConsumptionSeries(new List<CardTransaction> { ct }, latestMonth, 3);

            var firstPoint = series.First();
            firstPoint.Month.Should().Be(latestMonth.AddMonths(-2));
            firstPoint.Cards.Single().PesosAmount.Should().Be(0m);
        }

        [Fact]
        public void BuildMonthlyConsumptionSeries_CardWithoutActivityInWindow_IsNotIncluded()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var outsideWindow = MakeCardTransaction(1, 10, "NO", latestMonth.AddMonths(-6), 1, totalAmount: 5000m);

            var series = CardReportService.BuildMonthlyConsumptionSeries(new List<CardTransaction> { outsideWindow }, latestMonth, 3);

            series.SelectMany(p => p.Cards).Should().BeEmpty();
        }

        // ── BuildCategoryBreakdown ───────────────────────────────────────────────────────────────

        [Fact]
        public void BuildCategoryBreakdown_GroupsByCategoryAndCurrency_ForOneCard()
        {
            var startMonth = new DateTime(2026, 7, 1);
            var superA = MakeCardTransaction(1, 10, "NO", new DateTime(2026, 7, 5), 1, totalAmount: 1000m, categoryName: "Supermercado", transactionClassId: 1);
            var superB = MakeCardTransaction(2, 10, "NO", new DateTime(2026, 8, 5), 1, totalAmount: 500m, categoryName: "Supermercado", transactionClassId: 1);
            var ropa = MakeCardTransaction(3, 10, "NO", new DateTime(2026, 8, 5), 1, totalAmount: 300m, categoryName: "Ropa", transactionClassId: 2);
            var otherCard = MakeCardTransaction(4, 99, "NO", new DateTime(2026, 8, 5), 1, totalAmount: 999m, categoryName: "Supermercado", transactionClassId: 1);

            var result = CardReportService.BuildCategoryBreakdown(new List<CardTransaction> { superA, superB, ropa, otherCard }, cardId: 10, startMonth);

            result.Should().HaveCount(2);
            result.Single(c => c.TransactionClassId == 1).PesosAmount.Should().Be(1500m);
            result.Single(c => c.TransactionClassId == 2).PesosAmount.Should().Be(300m);
            result.First().TransactionClassId.Should().Be(1); // ordenado por monto descendente
        }

        [Fact]
        public void BuildCategoryBreakdown_ExcludesTransactionsBeforeWindow()
        {
            var startMonth = new DateTime(2026, 7, 1);
            var before = MakeCardTransaction(1, 10, "NO", new DateTime(2026, 6, 30), 1, totalAmount: 1000m);

            var result = CardReportService.BuildCategoryBreakdown(new List<CardTransaction> { before }, cardId: 10, startMonth);

            result.Should().BeEmpty();
        }

        // ── GetLiveInstallmentMonths: T8 extendido, con corte en currentMonth (no mira atrás) ───

        [Fact]
        public void GetLiveInstallmentMonths_FixedInstallments_AllFuture_ReturnsAllMonths()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", new DateTime(2026, 9, 1), 3); // sep, oct, nov

            var months = CardReportService.GetLiveInstallmentMonths(ct, new Dictionary<int, DateTime>(), currentMonth, 18);

            months.Should().BeEquivalentTo(new[] { new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), new DateTime(2026, 11, 1) });
        }

        [Fact]
        public void GetLiveInstallmentMonths_SomeAlreadyPaid_ReturnsOnlyUnpaid()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", new DateTime(2026, 7, 1), 4); // jul, ago, sep, oct
            var lastPaid = new Dictionary<int, DateTime> { { 10, new DateTime(2026, 8, 1) } }; // jul y ago pagados

            var months = CardReportService.GetLiveInstallmentMonths(ct, lastPaid, currentMonth, 18);

            months.Should().BeEquivalentTo(new[] { new DateTime(2026, 9, 1), new DateTime(2026, 10, 1) });
        }

        [Fact]
        public void GetLiveInstallmentMonths_OutsideForwardWindow_IsExcluded()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", new DateTime(2026, 9, 1), 3);

            var months = CardReportService.GetLiveInstallmentMonths(ct, new Dictionary<int, DateTime>(), currentMonth, monthsForward: 2);

            months.Should().BeEquivalentTo(new[] { new DateTime(2026, 9, 1), new DateTime(2026, 10, 1) });
        }

        [Fact]
        public void GetLiveInstallmentMonths_Recurrent_NeverPaid_ReturnsOnlyCurrentMonth()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "YES", new DateTime(2026, 5, 1), 0);

            var months = CardReportService.GetLiveInstallmentMonths(ct, new Dictionary<int, DateTime>(), currentMonth, 18);

            months.Should().BeEquivalentTo(new[] { currentMonth });
        }

        [Fact]
        public void GetLiveInstallmentMonths_Recurrent_PaidThroughCurrentMonth_ReturnsEmpty()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "YES", new DateTime(2026, 5, 1), 0);
            var lastPaid = new Dictionary<int, DateTime> { { 10, currentMonth } };

            var months = CardReportService.GetLiveInstallmentMonths(ct, lastPaid, currentMonth, 18);

            months.Should().BeEmpty();
        }

        // ── BuildFutureCommitment ────────────────────────────────────────────────────────────────

        [Fact]
        public void BuildFutureCommitment_StacksEachPurchaseIntoItsMonth_AndBuildsTimeline()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(7, 10, "NO", new DateTime(2026, 9, 1), 2, installmentAmount: 1000m, detail: "Heladera", cardName: "Visa");

            var result = CardReportService.BuildFutureCommitment(new List<CardTransaction> { ct }, new Dictionary<int, DateTime>(), currentMonth, 3);

            result.MonthlySeries.Should().HaveCount(3);
            result.MonthlySeries[0].Purchases.Should().ContainSingle(p => p.CardTransactionId == 7 && p.Amount == 1000m);
            result.MonthlySeries[1].Purchases.Should().ContainSingle(p => p.CardTransactionId == 7 && p.Amount == 1000m);
            result.MonthlySeries[2].Purchases.Should().BeEmpty(); // ya terminó en octubre

            result.Timeline.Should().ContainSingle();
            var entry = result.Timeline.Single();
            entry.StartMonth.Should().Be(new DateTime(2026, 9, 1));
            entry.EndMonth.Should().Be(new DateTime(2026, 10, 1));
            entry.RemainingInstallments.Should().Be(2);
            entry.Detail.Should().Be("Heladera");
        }

        [Fact]
        public void BuildFutureCommitment_NoLiveInstallments_TimelineIsEmpty()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", currentMonth.AddMonths(-3), 3);
            var lastPaid = new Dictionary<int, DateTime> { { 10, currentMonth.AddMonths(-1) } };

            var result = CardReportService.BuildFutureCommitment(new List<CardTransaction> { ct }, lastPaid, currentMonth, 12);

            result.Timeline.Should().BeEmpty();
            result.MonthlySeries.SelectMany(m => m.Purchases).Should().BeEmpty();
        }

        // ── BuildPromotionsReport ────────────────────────────────────────────────────────────────

        private static CardTransactionDiscount MakeDiscount(int id, DateTime creditDate, decimal amount, decimal amountApplied, decimal amountMaterialized, CardTransaction cardTransaction)
            => new CardTransactionDiscount
            {
                Id = id,
                CreditDate = creditDate,
                Amount = amount,
                AmountApplied = amountApplied,
                AmountMaterialized = amountMaterialized,
                CardTransactionId = cardTransaction.Id,
                CardTransaction = cardTransaction
            };

        [Fact]
        public void BuildPromotionsReport_TotalIsHistoricalNotLimitedToWindow()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", latestMonth, 1, assetName: "Peso Argentino");
            var oldDiscount = MakeDiscount(1, latestMonth.AddMonths(-20), 500m, 500m, 500m, ct); // fuera de la ventana de 12 meses

            var report = CardReportService.BuildPromotionsReport(new List<CardTransactionDiscount> { oldDiscount }, new List<CardTransaction>(), latestMonth, 12);

            report.TotalSavedPesos.Should().Be(500m);
            report.MonthlySeries.Sum(m => m.PesosAmount).Should().Be(0m); // no entra en la serie de 12 meses
        }

        [Fact]
        public void BuildPromotionsReport_PendingIncludesNotFullyApplied()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", latestMonth, 1, detail: "Compra con reintegro", cardName: "Visa");
            var fullyApplied = MakeDiscount(1, latestMonth, 100m, 100m, 100m, ct);
            var pendingToApply = MakeDiscount(2, latestMonth, 200m, 50m, 200m, ct);
            var pendingToCredit = MakeDiscount(3, latestMonth, 300m, 0m, 100m, ct);

            var report = CardReportService.BuildPromotionsReport(
                new List<CardTransactionDiscount> { fullyApplied, pendingToApply, pendingToCredit },
                new List<CardTransaction>(), latestMonth, 12);

            report.Pending.Should().HaveCount(2);
            report.Pending.Should().NotContain(p => p.DiscountId == 1);
            var p2 = report.Pending.Single(p => p.DiscountId == 2);
            p2.PendingToApply.Should().Be(150m);
            p2.PendingToCredit.Should().Be(0m);
            var p3 = report.Pending.Single(p => p.DiscountId == 3);
            p3.PendingToCredit.Should().Be(200m);
            p3.PendingToApply.Should().Be(100m);
        }

        [Fact]
        public void BuildPromotionsReport_PercentOfConsumption_NullWhenNoConsumptionInWindow()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", latestMonth, 1, assetName: "Peso Argentino");
            var discount = MakeDiscount(1, latestMonth, 100m, 0m, 0m, ct);

            var report = CardReportService.BuildPromotionsReport(new List<CardTransactionDiscount> { discount }, new List<CardTransaction>(), latestMonth, 12);

            report.PercentOfConsumptionPesos.Should().BeNull();
            report.PercentOfConsumptionDollars.Should().BeNull();
        }

        [Fact]
        public void BuildPromotionsReport_PercentOfConsumption_ComputedPerCurrency()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", latestMonth, 1, assetName: "Peso Argentino");
            var discount = MakeDiscount(1, latestMonth, 100m, 0m, 0m, ct);
            var consumption = MakeCardTransaction(2, 10, "NO", latestMonth, 1, totalAmount: 1000m, assetName: "Peso Argentino");

            var report = CardReportService.BuildPromotionsReport(new List<CardTransactionDiscount> { discount }, new List<CardTransaction> { consumption }, latestMonth, 12);

            report.PercentOfConsumptionPesos.Should().Be(10m); // 100 / 1000 * 100
            report.PercentOfConsumptionDollars.Should().BeNull();
        }
    }
}
