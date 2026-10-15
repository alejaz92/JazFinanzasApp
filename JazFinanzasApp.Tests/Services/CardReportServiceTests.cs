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

        // ── Devengado de tramos cerrados (plan-devengado-recurrentes.md, Fase 1): cambiar el precio de
        // un recurrente cierra el tramo vigente ("CLOSED") y abre uno nuevo; el tramo cerrado tiene que
        // seguir devengando en todos los meses en que estuvo activo, no una sola vez en su Date ───────

        private static CardTransaction MakeRecurrentTranche(
            int id, string repeat, DateTime firstInstallment, DateTime? lastInstallment, decimal price,
            int cardId = 10, DateTime? date = null, int transactionClassId = 1, string categoryName = "Suscripciones")
        {
            var ct = MakeCardTransaction(id, cardId, repeat, firstInstallment, 1, totalAmount: price, installmentAmount: price,
                date: date, transactionClassId: transactionClassId, categoryName: categoryName);
            ct.LastInstallment = lastInstallment;
            return ct;
        }

        [Fact]
        public void BuildCardEvolution_ClosedTranche_AccruesEveryMonthOfItsRange_AndNoneOutside()
        {
            var internet = MakeRecurrentTranche(1, "CLOSED", new DateTime(2026, 2, 1), new DateTime(2026, 4, 1), 28400m);

            var evolution = CardReportService.BuildCardEvolution(new List<CardTransaction> { internet }, cardId: 10, latestMonth: new DateTime(2026, 6, 1), monthsBack: 6);

            evolution.Select(p => p.PesosAmount).Should().Equal(0m, 28400m, 28400m, 28400m, 0m, 0m); // ene a jun
        }

        [Fact]
        public void BuildCardEvolution_ClosedTranche_LoadedBeforeItsFirstMonth_AccruesFromFirstInstallment()
        {
            // Seguro Departamento real: cargado en nov-2023, se empezó a cobrar en mar-2024.
            var tranche = MakeRecurrentTranche(1, "CLOSED", new DateTime(2024, 3, 1), new DateTime(2024, 5, 1), 5458m, date: new DateTime(2023, 11, 15));

            var evolution = CardReportService.BuildCardEvolution(new List<CardTransaction> { tranche }, cardId: 10, latestMonth: new DateTime(2024, 5, 1), monthsBack: 7);

            evolution.Select(p => p.PesosAmount).Should().Equal(0m, 0m, 0m, 0m, 5458m, 5458m, 5458m); // nov-2023 a may-2024
        }

        [Fact]
        public void BuildCardEvolution_ClosedTranche_WithoutLastInstallment_AccruesOnlyItsFirstMonth()
        {
            // Xbox Game Pass real (id 17): cerrado sin fecha de cierre (D3).
            var tranche = MakeRecurrentTranche(1, "CLOSED", new DateTime(2026, 2, 1), null, 1.06m);

            var evolution = CardReportService.BuildCardEvolution(new List<CardTransaction> { tranche }, cardId: 10, latestMonth: new DateTime(2026, 4, 1), monthsBack: 4);

            evolution.Select(p => p.PesosAmount).Should().Equal(0m, 1.06m, 0m, 0m); // ene a abr
        }

        [Fact]
        public void BuildCardEvolution_ActiveRecurrent_WithPlaceholderLastInstallment_KeepsAccruingEveryMonth()
        {
            // Claude Pro, Patreon y Pedidos ya plus reales: vigentes con LastInstallment = 0001-01-01 (D4).
            var tranche = MakeRecurrentTranche(1, "YES", new DateTime(2026, 2, 1), new DateTime(1, 1, 1), 20m);

            var evolution = CardReportService.BuildCardEvolution(new List<CardTransaction> { tranche }, cardId: 10, latestMonth: new DateTime(2026, 4, 1), monthsBack: 4);

            evolution.Select(p => p.PesosAmount).Should().Equal(0m, 20m, 20m, 20m); // ene a abr
        }

        [Fact]
        public void BuildCardEvolution_ChainOfPriceChanges_ShowsEachPriceInItsOwnMonths()
        {
            // El escenario de demo del plan: Spotify a 4.000 desde ene, 5.500 desde abr, 7.000 desde ago.
            var chain = new List<CardTransaction>
            {
                MakeRecurrentTranche(1, "CLOSED", new DateTime(2026, 1, 1), new DateTime(2026, 3, 1), 4000m),
                MakeRecurrentTranche(2, "CLOSED", new DateTime(2026, 4, 1), new DateTime(2026, 7, 1), 5500m),
                MakeRecurrentTranche(3, "YES", new DateTime(2026, 8, 1), null, 7000m)
            };

            var evolution = CardReportService.BuildCardEvolution(chain, cardId: 10, latestMonth: new DateTime(2026, 10, 1), monthsBack: 10);

            evolution.Select(p => p.PesosAmount).Should().Equal(
                4000m, 4000m, 4000m, 5500m, 5500m, 5500m, 5500m, 7000m, 7000m, 7000m); // ene a oct
        }

        [Fact]
        public void BuildCategoryBreakdown_ClosedTranche_SumsInstallmentAmountOncePerActiveMonthInsideTheWindow()
        {
            // Tramo de ene a jul; la ventana arranca en abr, así que entran abr, may, jun y jul.
            var tranche = MakeRecurrentTranche(1, "CLOSED", new DateTime(2026, 1, 1), new DateTime(2026, 7, 1), 5500m);

            var result = CardReportService.BuildCategoryBreakdown(new List<CardTransaction> { tranche }, cardId: 10,
                startMonth: new DateTime(2026, 4, 1), latestMonth: new DateTime(2026, 10, 1));

            result.Should().ContainSingle();
            result.Single().PesosAmount.Should().Be(22000m);
        }

        [Fact]
        public void BuildMonthlyConsumptionSeries_CardWhoseOnlyConsumptionIsAClosedTrancheStartedBeforeTheWindow_IsIncluded()
        {
            var tranche = MakeRecurrentTranche(1, "CLOSED", new DateTime(2025, 5, 1), new DateTime(2026, 3, 1), 28400m);

            var series = CardReportService.BuildMonthlyConsumptionSeries(new List<CardTransaction> { tranche }, latestMonth: new DateTime(2026, 4, 1), monthsBack: 3);

            series.Select(p => p.Cards.Single().PesosAmount).Should().Equal(28400m, 28400m, 0m); // feb, mar, abr
        }

        [Fact]
        public void BuildCardEvolution_PurchaseInInstallments_StillAccruesItsTotalOnceInItsPurchaseMonth()
        {
            var purchase = MakeCardTransaction(1, 10, "NO", new DateTime(2026, 2, 1), 3, totalAmount: 3000m, installmentAmount: 1000m, date: new DateTime(2026, 1, 20));
            purchase.LastInstallment = new DateTime(2026, 4, 1);

            var evolution = CardReportService.BuildCardEvolution(new List<CardTransaction> { purchase }, cardId: 10, latestMonth: new DateTime(2026, 4, 1), monthsBack: 4);

            evolution.Select(p => p.PesosAmount).Should().Equal(3000m, 0m, 0m, 0m); // ene a abr
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

        // Corrección 2026-09-05, quinta ronda: un recurrente sin fin ya no corta en el mes en curso —
        // se proyecta en TODOS los meses de la ventana (bug real: un gasto real como una prepaga
        // aparecía comprometido un solo mes en vez de en los 18).
        [Fact]
        public void GetLiveInstallmentMonths_Recurrent_NeverPaid_ProjectsEveryMonthInTheWindow()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "YES", new DateTime(2026, 5, 1), 0);

            var months = CardReportService.GetLiveInstallmentMonths(ct, new Dictionary<int, DateTime>(), currentMonth, 3);

            months.Should().BeEquivalentTo(new[] { new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), new DateTime(2026, 11, 1) });
        }

        [Fact]
        public void GetLiveInstallmentMonths_Recurrent_PaidThroughCurrentMonth_ProjectsFromNextMonthOnward()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "YES", new DateTime(2026, 5, 1), 0);
            var lastPaid = new Dictionary<int, DateTime> { { 10, currentMonth } }; // septiembre ya pagado

            var months = CardReportService.GetLiveInstallmentMonths(ct, lastPaid, currentMonth, 3);

            months.Should().BeEquivalentTo(new[] { new DateTime(2026, 10, 1), new DateTime(2026, 11, 1) }); // sept no, ya pagado
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

        // Corrección 2026-09-05, cuarta ronda: el gráfico de "cuotas por vencer" pasa a agrupar por
        // categoría en vez de por compra — necesita esta info en cada FutureCommitmentPurchaseAmountDTO.
        [Fact]
        public void BuildFutureCommitment_PurchaseAmount_CarriesCategoryInfo()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeCardTransaction(1, 10, "NO", currentMonth, 1, transactionClassId: 4, categoryName: "Electro");

            var result = CardReportService.BuildFutureCommitment(new List<CardTransaction> { ct }, new Dictionary<int, DateTime>(), currentMonth, 3);

            var purchase = result.MonthlySeries[0].Purchases.Single();
            purchase.TransactionClassId.Should().Be(4);
            purchase.TransactionClassName.Should().Be("Electro");
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

        // Corrección 2026-09-05, quinta ronda: filtro para sacar los recurrentes de la proyección —
        // una vez que se proyectan en todos los meses, pueden tapar las compras en cuotas puntuales.
        [Fact]
        public void BuildFutureCommitment_IncludeRecurringFalse_ExcludesRecurrentPurchases()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var recurrent = MakeCardTransaction(1, 10, "YES", new DateTime(2026, 5, 1), 0, detail: "Swiss Medical");
            var fixedInstallment = MakeCardTransaction(2, 10, "NO", currentMonth, 2, detail: "Heladera");

            var result = CardReportService.BuildFutureCommitment(new List<CardTransaction> { recurrent, fixedInstallment }, new Dictionary<int, DateTime>(), currentMonth, 3, includeRecurring: false);

            result.Timeline.Should().ContainSingle(t => t.Detail == "Heladera");
            result.MonthlySeries.SelectMany(m => m.Purchases).Should().OnlyContain(p => p.Detail == "Heladera");
        }

        // ── Tramos cerrados en el compromiso futuro (plan-devengado-recurrentes.md, D8) ───────────

        [Fact]
        public void GetLiveInstallmentMonths_ClosedTrancheWithFutureEnd_ProjectsUnpaidMonthsUntilItsLastMonthOnly()
        {
            // "La doy de baja a partir de diciembre": el tramo se cobra hasta nov y no más.
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeRecurrentTranche(1, "CLOSED", new DateTime(2026, 1, 1), new DateTime(2026, 11, 1), 7000m);
            var lastPaid = new Dictionary<int, DateTime> { { 10, new DateTime(2026, 8, 1) } };

            var months = CardReportService.GetLiveInstallmentMonths(ct, lastPaid, currentMonth, 18);

            months.Should().Equal(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), new DateTime(2026, 11, 1));
        }

        [Fact]
        public void GetLiveInstallmentMonths_ClosedTrancheAlreadyOver_ProjectsNothing()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var ct = MakeRecurrentTranche(1, "CLOSED", new DateTime(2026, 1, 1), new DateTime(2026, 7, 1), 7000m);

            var months = CardReportService.GetLiveInstallmentMonths(ct, new Dictionary<int, DateTime>(), currentMonth, 18);

            months.Should().BeEmpty();
        }

        [Fact]
        public void BuildFutureCommitment_IncludeRecurringFalse_AlsoExcludesClosedTranchesWithFutureMonths()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var closing = MakeRecurrentTranche(1, "CLOSED", new DateTime(2026, 1, 1), new DateTime(2026, 11, 1), 7000m);
            closing.Detail = "Spotify";
            var fixedInstallment = MakeCardTransaction(2, 10, "NO", currentMonth, 2, detail: "Heladera");

            var result = CardReportService.BuildFutureCommitment(new List<CardTransaction> { closing, fixedInstallment }, new Dictionary<int, DateTime>(), currentMonth, 3, includeRecurring: false);

            result.Timeline.Should().ContainSingle(t => t.Detail == "Heladera");
            result.MonthlySeries.SelectMany(m => m.Purchases).Should().OnlyContain(p => p.Detail == "Heladera");
        }

        [Fact]
        public void BuildFutureCommitment_IncludeRecurringTrue_KeepsRecurrentPurchases()
        {
            var currentMonth = new DateTime(2026, 9, 1);
            var recurrent = MakeCardTransaction(1, 10, "YES", new DateTime(2026, 5, 1), 0, detail: "Swiss Medical");

            var result = CardReportService.BuildFutureCommitment(new List<CardTransaction> { recurrent }, new Dictionary<int, DateTime>(), currentMonth, 3, includeRecurring: true);

            result.Timeline.Should().ContainSingle(t => t.Detail == "Swiss Medical");
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

        // Corrección 2026-09-05, sexta ronda: filtro por tarjeta (0 = todas, mismo criterio que
        // MonthSummary/GetCardTransactionsToPay) — pedido para poder ver el compromiso futuro de una
        // sola tarjeta, igual que ya se puede en General y en Por tarjeta.
        [Fact]
        public async Task GetFutureCommitmentAsync_FiltersByCardId_WhenGiven()
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var peso = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "$", Color = "#111" };
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#222" };
            var ctCard10 = MakeCardTransaction(1, 10, "NO", today, 2, detail: "De la 10", cardName: "Visa");
            var ctCard20 = MakeCardTransaction(2, 20, "NO", today, 2, detail: "De la 20", cardName: "Master");

            SetupCurrencyMocks(peso, dollar, referenceAsset: peso, new List<CardTransaction> { ctCard10, ctCard20 });
            _cardPaymentRepoMock.Setup(r => r.GetLastPaidMonthByCardAsync(UserId)).ReturnsAsync(new Dictionary<int, DateTime>());

            var result = await _sut.GetFutureCommitmentAsync(UserId, peso.Id, cardId: 10);

            result.Timeline.Should().ContainSingle(t => t.Detail == "De la 10");
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
