using FluentAssertions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 14 (Tarjetas): en su mayoría los métodos puros del servicio — mismo criterio que
    // NetWorthReportServiceTests para T8 (CountLiveInstallmentMonths), sin mocks de repositorio.
    // Corrección 2026-09-05 (conversión de moneda en GetGeneralAsync) sí necesita mocks, mismo
    // patrón que NetWorthReportServiceTests para GetLiveCardDebtInDollarsAsync.
    public class CardReportServiceTests
    {
        private const int UserId = 1;

        private readonly Mock<ICardRepository> _cardRepoMock = new();
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock = new();
        private readonly Mock<ICardPaymentRepository> _cardPaymentRepoMock = new();
        private readonly Mock<ICardTransactionDiscountRepository> _cardTransactionDiscountRepoMock = new();
        private readonly Mock<IAssetRepository> _assetRepoMock = new();
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock = new();
        private readonly CardReportService _sut;

        public CardReportServiceTests()
        {
            _sut = new CardReportService(
                _cardRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _cardPaymentRepoMock.Object,
                _cardTransactionDiscountRepoMock.Object,
                _assetRepoMock.Object,
                _assetQuoteRepoMock.Object);
        }

        // Assets y CardTransactions comunes a los 4 endpoints con conversión de moneda.
        private void SetupCurrencyMocks(Asset peso, Asset dollar, Asset referenceAsset, List<CardTransaction> transactions)
        {
            _assetRepoMock.Setup(r => r.GetByIdAsync(referenceAsset.Id)).ReturnsAsync(referenceAsset);
            _assetRepoMock.Setup(r => r.GetAssetByNameAsync("Peso Argentino")).ReturnsAsync(peso);
            _assetRepoMock.Setup(r => r.GetAssetByNameAsync("Dolar Estadounidense")).ReturnsAsync(dollar);
            _cardTransactionRepoMock.Setup(r => r.GetByUserIdWithDetailsAsync(UserId)).ReturnsAsync(transactions);
        }

        // Deja armado lo mínimo para que GetGeneralAsync corra completo: los assets, las
        // CardTransactions y un resumen del mes vacío (BuildMonthSummaryAsync no es lo que se
        // está probando acá).
        private void SetupGeneralAsyncMocks(Asset peso, Asset dollar, Asset referenceAsset, List<CardTransaction> transactions, DateTime today)
        {
            SetupCurrencyMocks(peso, dollar, referenceAsset, transactions);
            _cardTransactionRepoMock.Setup(r => r.GetCardTransactionsToPay(0, today, UserId)).ReturnsAsync(new List<CardTransaction>());
        }

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

        // ── Devengado de recurrentes (Fase 15, encontrado al revisar "General" en el navegador): una
        // suscripción vieja tiene que seguir devengando todos los meses que sigue activa, no solo el
        // mes en que se cargó la fila ──────────────────────────────────────────────────────────────

        [Fact]
        public void BuildMonthlyConsumptionSeries_Recurrent_AccruesEveryMonthSinceItStarted_NotOnlyItsOwnDate()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            // Cargada hace casi dos años (fecha de la fila), pero arrancó en mayo 2024 y sigue activa.
            var netflix = MakeCardTransaction(1, 10, "YES", new DateTime(2024, 5, 1), 0, installmentAmount: 15m, assetName: "Dolar Estadounidense", date: new DateTime(2024, 11, 28));

            var series = CardReportService.BuildMonthlyConsumptionSeries(new List<CardTransaction> { netflix }, latestMonth, 3);

            series.Should().OnlyContain(p => p.Cards.Single().DollarsAmount == 15m);
        }

        [Fact]
        public void BuildMonthlyConsumptionSeries_Recurrent_NotYetStarted_ContributesNothing()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var futureSubscription = MakeCardTransaction(1, 10, "YES", new DateTime(2026, 10, 1), 0, installmentAmount: 15m, assetName: "Dolar Estadounidense");

            var series = CardReportService.BuildMonthlyConsumptionSeries(new List<CardTransaction> { futureSubscription }, latestMonth, 3);

            series.SelectMany(p => p.Cards).Should().BeEmpty();
        }

        [Fact]
        public void BuildCategoryBreakdown_Recurrent_SumsInstallmentAmountOncePerActiveMonth()
        {
            var startMonth = new DateTime(2026, 7, 1);
            var latestMonth = new DateTime(2026, 9, 1);
            var netflix = MakeCardTransaction(1, 10, "YES", new DateTime(2024, 5, 1), 0, installmentAmount: 15m, assetName: "Dolar Estadounidense", categoryName: "Viajes");

            var result = CardReportService.BuildCategoryBreakdown(new List<CardTransaction> { netflix }, cardId: 10, startMonth, latestMonth);

            result.Should().ContainSingle();
            result.Single().DollarsAmount.Should().Be(45m); // 15 x 3 meses (jul, ago, sep)
        }

        [Fact]
        public void BuildCardEvolution_Recurrent_AccruesEveryMonthSinceItStarted()
        {
            var latestMonth = new DateTime(2026, 9, 1);
            var netflix = MakeCardTransaction(1, 10, "YES", new DateTime(2024, 5, 1), 0, installmentAmount: 15m, assetName: "Dolar Estadounidense");

            var evolution = CardReportService.BuildCardEvolution(new List<CardTransaction> { netflix }, cardId: 10, latestMonth, 3);

            evolution.Should().OnlyContain(p => p.DollarsAmount == 15m);
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

            var result = CardReportService.BuildCategoryBreakdown(new List<CardTransaction> { superA, superB, ropa, otherCard }, cardId: 10, startMonth, latestMonth: new DateTime(2026, 9, 1));

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

            var result = CardReportService.BuildCategoryBreakdown(new List<CardTransaction> { before }, cardId: 10, startMonth, latestMonth: new DateTime(2026, 9, 1));

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

        // ── GetGeneralAsync: conversión de MonthlySeries a la moneda de referencia elegida ──────
        // (corrección 2026-09-05: el selector de moneda de la barra de Reportes no hacía nada acá)

        [Fact]
        public async Task GetGeneralAsync_ConvertsPesosAmountToReferenceCurrency_WhenReferenceIsDollar()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var peso = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "$", Color = "#111" };
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#222" };
            var ct = MakeCardTransaction(1, 10, "NO", today, 1, totalAmount: 1000m, assetName: "Peso Argentino", cardName: "Visa");

            SetupGeneralAsyncMocks(peso, dollar, referenceAsset: dollar, new List<CardTransaction> { ct }, today);
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, today, "TARJETA")).ReturnsAsync(1000m); // 1000 $ = 1 USD ese mes

            var result = await _sut.GetGeneralAsync(UserId, dollar.Id);

            result.ReferenceAssetSymbol.Should().Be("US$");
            var lastPoint = result.MonthlySeries.Last();
            lastPoint.Cards.Single().PesosAmount.Should().Be(1m); // 1000 pesos / 1000 = 1 dolar
        }

        [Fact]
        public async Task GetGeneralAsync_ConvertsDollarsAmountToReferenceCurrency_WhenReferenceIsPeso()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var peso = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "$", Color = "#111" };
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#222" };
            var ct = MakeCardTransaction(1, 10, "NO", today, 1, totalAmount: 15m, assetName: "Dolar Estadounidense", cardName: "Visa");

            SetupGeneralAsyncMocks(peso, dollar, referenceAsset: peso, new List<CardTransaction> { ct }, today);
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, today, "TARJETA")).ReturnsAsync(1200m); // 1 USD = 1200 $ ese mes

            var result = await _sut.GetGeneralAsync(UserId, peso.Id);

            var lastPoint = result.MonthlySeries.Last();
            lastPoint.Cards.Single().DollarsAmount.Should().Be(18000m); // 15 USD x 1200
        }

        [Fact]
        public async Task GetGeneralAsync_ReferenceEqualsNativeCurrency_NoConversionApplied()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var peso = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "$", Color = "#111" };
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#222" };
            var ct = MakeCardTransaction(1, 10, "NO", today, 1, totalAmount: 1000m, assetName: "Peso Argentino", cardName: "Visa");

            SetupGeneralAsyncMocks(peso, dollar, referenceAsset: peso, new List<CardTransaction> { ct }, today);

            var result = await _sut.GetGeneralAsync(UserId, peso.Id);

            // Sin cambios: BuildMonthSummaryAsync (resumen del mes, no la serie) sí pide una
            // cotización propia para su tabla de cash-flow — no es la conversión bajo prueba acá.
            result.MonthlySeries.Last().Cards.Single().PesosAmount.Should().Be(1000m);
        }

        [Fact]
        public async Task GetGeneralAsync_UsesEachMonthsOwnHistoricalQuote_NotTodays()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var twoMonthsAgo = today.AddMonths(-2);
            var peso = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "$", Color = "#111" };
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#222" };
            var oldCt = MakeCardTransaction(1, 10, "NO", twoMonthsAgo, 1, totalAmount: 500m, assetName: "Peso Argentino", cardName: "Visa");

            SetupGeneralAsyncMocks(peso, dollar, referenceAsset: dollar, new List<CardTransaction> { oldCt }, today);
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, twoMonthsAgo, "TARJETA")).ReturnsAsync(500m); // cotización de hace 2 meses
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, today, "TARJETA")).ReturnsAsync(2000m); // cotización de hoy, muy distinta

            var result = await _sut.GetGeneralAsync(UserId, dollar.Id);

            var pointTwoMonthsAgo = result.MonthlySeries.Single(p => p.Month == twoMonthsAgo);
            pointTwoMonthsAgo.Cards.Single().PesosAmount.Should().Be(1m); // 500 / 500, con la cotización de ESE mes
        }

        // ── GetByCardAsync: conversión en consumo del mes, ByCategory y evolución ───────────────

        [Fact]
        public async Task GetByCardAsync_ConvertsCurrentMonthByCategoryAndEvolution_ToReferenceCurrency()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var peso = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "$", Color = "#111" };
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#222" };
            var ct = MakeCardTransaction(1, 10, "NO", today, 1, totalAmount: 1000m, assetName: "Peso Argentino", cardName: "Visa", categoryName: "Super");

            _cardRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Card { Id = 10, Name = "Visa", UserId = UserId });
            SetupCurrencyMocks(peso, dollar, referenceAsset: dollar, new List<CardTransaction> { ct });
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, today, "TARJETA")).ReturnsAsync(1000m);

            var result = await _sut.GetByCardAsync(UserId, cardId: 10, assetId: dollar.Id);

            result.CurrentMonthPesos.Should().Be(1m); // 1000 / 1000
            result.ByCategory.Single().PesosAmount.Should().Be(1m);
            result.MonthlyEvolution.Last().PesosAmount.Should().Be(1m);
        }

        [Fact]
        public async Task GetByCardAsync_WrongUser_ThrowsUnauthorized()
        {
            _cardRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Card { Id = 10, Name = "Visa", UserId = 999 });

            var act = async () => await _sut.GetByCardAsync(UserId, cardId: 10, assetId: 1);

            await act.Should().ThrowAsync<JazFinanzasApp.API.Business.Exceptions.UnauthorizedDomainException>();
        }

        // ── GetFutureCommitmentAsync: conversión con la cotización de hoy (meses futuros) ───────

        [Fact]
        public async Task GetFutureCommitmentAsync_ConvertsPurchaseAmounts_ToReferenceCurrency()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var peso = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "$", Color = "#111" };
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#222" };
            var ct = MakeCardTransaction(1, 10, "NO", today, 2, installmentAmount: 1000m, assetName: "Peso Argentino", detail: "Heladera", cardName: "Visa");

            SetupCurrencyMocks(peso, dollar, referenceAsset: dollar, new List<CardTransaction> { ct });
            _cardPaymentRepoMock.Setup(r => r.GetLastPaidMonthByCardAsync(UserId)).ReturnsAsync(new Dictionary<int, DateTime>());
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, today, "TARJETA")).ReturnsAsync(1000m);

            var result = await _sut.GetFutureCommitmentAsync(UserId, dollar.Id);

            result.MonthlySeries[0].Purchases.Single().Amount.Should().Be(1m); // 1000 / 1000
            result.Timeline.Single().InstallmentAmount.Should().Be(1m);
        }

        // ── GetPromotionsAsync: TotalSaved (hoy), MonthlySeries (por mes), Pending (su CreditDate) ─

        [Fact]
        public async Task GetPromotionsAsync_ConvertsTotalMonthlySeriesAndPending_EachWithItsOwnDate()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var creditDate = today.AddMonths(-3);
            var peso = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "$", Color = "#111" };
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#222" };
            var ct = MakeCardTransaction(1, 10, "NO", creditDate, 1, assetName: "Peso Argentino", cardName: "Visa");
            var discount = MakeDiscount(1, creditDate, amount: 500m, amountApplied: 0m, amountMaterialized: 0m, ct);

            SetupCurrencyMocks(peso, dollar, referenceAsset: dollar, new List<CardTransaction>());
            _cardTransactionDiscountRepoMock.Setup(r => r.GetByUserIdWithCardTransactionAsync(UserId)).ReturnsAsync(new List<CardTransactionDiscount> { discount });
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, today, "TARJETA")).ReturnsAsync(1000m); // cotización de hoy (TotalSaved)
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, creditDate, "TARJETA")).ReturnsAsync(500m); // cotización de hace 3 meses (Pending y MonthlySeries)

            var result = await _sut.GetPromotionsAsync(UserId, dollar.Id);

            result.TotalSavedPesos.Should().Be(0.5m); // 500 / 1000 (cotización de HOY)
            result.MonthlySeries.Single(m => m.Month == creditDate).PesosAmount.Should().Be(1m); // 500 / 500 (cotización de ESE mes)
            result.Pending.Single().PendingToCredit.Should().Be(1m); // 500 / 500 (cotización de su propio CreditDate)
        }
    }
}
