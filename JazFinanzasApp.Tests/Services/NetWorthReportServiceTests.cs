using FluentAssertions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class NetWorthReportServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<ICardPaymentRepository> _cardPaymentRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock;
        private readonly Mock<IAsset_UserRepository> _asset_UserRepoMock;
        private readonly NetWorthReportService _sut;

        private const int UserId = 1;

        public NetWorthReportServiceTests()
        {
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _cardPaymentRepoMock = new Mock<ICardPaymentRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _assetQuoteRepoMock = new Mock<IAssetQuoteRepository>();
            _asset_UserRepoMock = new Mock<IAsset_UserRepository>();

            _sut = new NetWorthReportService(
                _transactionRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _cardPaymentRepoMock.Object,
                _assetRepoMock.Object,
                _assetQuoteRepoMock.Object,
                _asset_UserRepoMock.Object);
        }

        private static CardTransaction MakeCardTransaction(int cardId, string repeat, DateTime firstInstallment, int installments,
            decimal installmentAmount = 1000m, int assetId = 1, DateTime? date = null) => new CardTransaction
            {
                CardId = cardId,
                Repeat = repeat,
                FirstInstallment = firstInstallment,
                Installments = installments,
                InstallmentAmount = installmentAmount,
                AssetId = assetId,
                Date = date ?? firstInstallment
            };

        // ── T8: deuda de tarjeta viva — CountLiveInstallmentMonths (lógica pura) ────────────────

        [Fact]
        public void CountLiveInstallmentMonths_FixedInstallments_AllInFuture_CountsAll()
        {
            var currentMonth = new DateTime(2026, 8, 1);
            var ct = MakeCardTransaction(1, "NO", new DateTime(2026, 6, 1), 3); // 06, 07, 08

            var live = NetWorthReportService.CountLiveInstallmentMonths(ct, new Dictionary<int, DateTime>(), currentMonth);

            live.Should().Be(3);
        }

        [Fact]
        public void CountLiveInstallmentMonths_FixedInstallments_SomeAlreadyPaid_CountsOnlyUnpaid()
        {
            var currentMonth = new DateTime(2026, 8, 1);
            var ct = MakeCardTransaction(1, "NO", new DateTime(2026, 6, 1), 3); // 06, 07, 08
            var lastPaid = new Dictionary<int, DateTime> { { 1, new DateTime(2026, 7, 1) } }; // 06 y 07 pagados

            var live = NetWorthReportService.CountLiveInstallmentMonths(ct, lastPaid, currentMonth);

            live.Should().Be(1); // solo queda 08
        }

        [Fact]
        public void CountLiveInstallmentMonths_FixedInstallments_AllAlreadyPaid_CountsZero()
        {
            var currentMonth = new DateTime(2026, 8, 1);
            var ct = MakeCardTransaction(2, "CLOSED", new DateTime(2026, 1, 1), 6); // 01..06
            var lastPaid = new Dictionary<int, DateTime> { { 2, new DateTime(2026, 6, 1) } };

            var live = NetWorthReportService.CountLiveInstallmentMonths(ct, lastPaid, currentMonth);

            live.Should().Be(0);
        }

        [Fact]
        public void CountLiveInstallmentMonths_Recurrent_NeverPaid_CountsOne()
        {
            var currentMonth = new DateTime(2026, 8, 1);
            var ct = MakeCardTransaction(3, "YES", new DateTime(2026, 5, 1), 0);

            var live = NetWorthReportService.CountLiveInstallmentMonths(ct, new Dictionary<int, DateTime>(), currentMonth);

            live.Should().Be(1);
        }

        [Fact]
        public void CountLiveInstallmentMonths_Recurrent_PaidThroughCurrentMonth_CountsZero()
        {
            var currentMonth = new DateTime(2026, 8, 1);
            var ct = MakeCardTransaction(3, "YES", new DateTime(2026, 5, 1), 0);
            var lastPaid = new Dictionary<int, DateTime> { { 3, new DateTime(2026, 8, 1) } };

            var live = NetWorthReportService.CountLiveInstallmentMonths(ct, lastPaid, currentMonth);

            live.Should().Be(0);
        }

        [Fact]
        public void CountLiveInstallmentMonths_Recurrent_PaidLastMonth_CountsOneForCurrentMonth()
        {
            var currentMonth = new DateTime(2026, 8, 1);
            var ct = MakeCardTransaction(3, "YES", new DateTime(2026, 5, 1), 0);
            var lastPaid = new Dictionary<int, DateTime> { { 3, new DateTime(2026, 7, 1) } };

            var live = NetWorthReportService.CountLiveInstallmentMonths(ct, lastPaid, currentMonth);

            live.Should().Be(1);
        }

        // ── T8: GetLiveCardDebtInDollarsAsync (integración con conversión de moneda) ────────────

        [Fact]
        public async Task GetLiveCardDebtInDollarsAsync_PesoInstallments_ConvertsUsingConsumptionDateQuote()
        {
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var ct = MakeCardTransaction(1, "NO", currentMonth, 1, installmentAmount: 2000m, assetId: 10, date: currentMonth.AddDays(2));

            _cardTransactionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CardTransaction, bool>>>()))
                .ReturnsAsync(new List<CardTransaction> { ct });
            _cardPaymentRepoMock.Setup(r => r.GetLastPaidMonthByCardAsync(UserId)).ReturnsAsync(new Dictionary<int, DateTime>());
            _assetRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Asset { Id = 10, Name = "Peso Argentino", Symbol = "$" });
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(10, ct.Date, "TARJETA")).ReturnsAsync(1000m);

            var debt = await _sut.GetLiveCardDebtInDollarsAsync(UserId);

            debt.Should().Be(2m); // 2000 pesos / 1000 = 2 dolares
        }

        [Fact]
        public async Task GetLiveCardDebtInDollarsAsync_DollarInstallments_NoConversionNeeded()
        {
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var ct = MakeCardTransaction(1, "NO", currentMonth, 1, installmentAmount: 50m, assetId: 2, date: currentMonth.AddDays(2));

            _cardTransactionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CardTransaction, bool>>>()))
                .ReturnsAsync(new List<CardTransaction> { ct });
            _cardPaymentRepoMock.Setup(r => r.GetLastPaidMonthByCardAsync(UserId)).ReturnsAsync(new Dictionary<int, DateTime>());
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$" });

            var debt = await _sut.GetLiveCardDebtInDollarsAsync(UserId);

            debt.Should().Be(50m);
            _assetQuoteRepoMock.Verify(r => r.GetQuotePrice(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetLiveCardDebtInDollarsAsync_NoLiveInstallments_ReturnsZero()
        {
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var ct = MakeCardTransaction(1, "NO", currentMonth.AddMonths(-3), 3); // 3 cuotas, todas ya deberían estar pagas
            var lastPaid = new Dictionary<int, DateTime> { { 1, currentMonth.AddMonths(-1) } };

            _cardTransactionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CardTransaction, bool>>>()))
                .ReturnsAsync(new List<CardTransaction> { ct });
            _cardPaymentRepoMock.Setup(r => r.GetLastPaidMonthByCardAsync(UserId)).ReturnsAsync(lastPaid);

            var debt = await _sut.GetLiveCardDebtInDollarsAsync(UserId);

            debt.Should().Be(0m);
            _assetRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        // ── D-E / T7: neto = bruto - deuda viva, sin tocar el bruto ─────────────────────────────

        [Fact]
        public async Task GetGeneralAsync_SubtractsLiveCardDebtFromGrossBalance()
        {
            var dollar = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "US$", Color = "#000" };
            _asset_UserRepoMock.Setup(r => r.GetReferenceAssetsAsync(UserId)).ReturnsAsync(new List<Asset_User>());
            _assetRepoMock.Setup(r => r.GetAssetByNameAsync("Dolar Estadounidense")).ReturnsAsync(dollar);

            _cardTransactionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CardTransaction, bool>>>()))
                .ReturnsAsync(new List<CardTransaction>());
            _cardPaymentRepoMock.Setup(r => r.GetLastPaidMonthByCardAsync(UserId)).ReturnsAsync(new Dictionary<int, DateTime>());
            _transactionRepoMock.Setup(r => r.GetOldestQuoteDateForHoldingsAsync(UserId)).ReturnsAsync((DateTime?)null);

            _transactionRepoMock.Setup(r => r.GetTotalsBalanceByUserAsync(UserId, dollar))
                .ReturnsAsync(new TotalsBalanceResult { Asset = "Dolar Estadounidense", Symbol = "US$", Color = "#000", Balance = 1000m });
            _transactionRepoMock.Setup(r => r.GetReferenceAssetRateAsync(dollar)).ReturnsAsync((1m, (DateTime?)null));

            // Forzamos deuda viva > 0 sin pasar por todo el cálculo: una cuota en dólares, mes en curso, sin pagos.
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var ct = MakeCardTransaction(1, "NO", currentMonth, 1, installmentAmount: 150m, assetId: 2, date: currentMonth.AddDays(1));
            _cardTransactionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CardTransaction, bool>>>()))
                .ReturnsAsync(new List<CardTransaction> { ct });
            _assetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(dollar);

            var result = (await _sut.GetGeneralAsync(UserId)).Single();

            result.GrossBalance.Should().Be(1000m);
            result.CardDebt.Should().Be(150m);
            result.NetBalance.Should().Be(850m);
        }
    }
}
