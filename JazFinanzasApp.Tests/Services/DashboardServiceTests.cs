using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Card;
using JazFinanzasApp.API.Business.DTO.CardReport;
using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.DTO.Dashboard;
using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.DTO.NetWorth;
using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.DTO.Trip;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 16 (Bloque D — Panorama e Inicio, backend): en su mayoría los métodos puros del servicio
    // (mismo criterio que CardReportServiceTests/NetWorthReportServiceTests) más una tanda de tests
    // de composición con los servicios de los bloques anteriores mockeados (mismo patrón que
    // TripServiceTests con IReportService), para verificar que GetDashboardAsync arma el DTO final
    // con lo que cada uno devuelve, sin recalcular nada.
    public class DashboardServiceTests
    {
        private const int UserId = 1;
        private static readonly DateTime Today = new(2026, 9, 15);

        private readonly Mock<INetWorthReportService> _netWorthReportServiceMock = new();
        private readonly Mock<IIncomeExpenseReportService> _incomeExpenseReportServiceMock = new();
        private readonly Mock<ICardReportService> _cardReportServiceMock = new();
        private readonly Mock<ICardService> _cardServiceMock = new();
        private readonly Mock<ISharedEventService> _sharedEventServiceMock = new();
        private readonly Mock<ITripService> _tripServiceMock = new();
        private readonly Mock<IAssetRepository> _assetRepoMock = new();
        private readonly DashboardService _sut;

        private static readonly Asset PesoAsset = new() { Id = 1, Name = "Peso Argentino", Symbol = "$", AssetTypeId = 1 };

        public DashboardServiceTests()
        {
            _sut = new DashboardService(
                _netWorthReportServiceMock.Object,
                _incomeExpenseReportServiceMock.Object,
                _cardReportServiceMock.Object,
                _cardServiceMock.Object,
                _sharedEventServiceMock.Object,
                _tripServiceMock.Object,
                _assetRepoMock.Object);
        }

        // ── GetCardDueStatus: puerto de card-due-status.util.ts ─────────────────────────────────

        private static CardDTO MakeCard(DateTime? closing, DateTime? due, bool isPaid = false, int id = 1)
            => new() { Id = id, Name = "Visa", NextClosingDate = closing, NextDueDate = due, IsCurrentPeriodPaid = isPaid };

        [Fact]
        public void GetCardDueStatus_WithoutDates_ReturnsNinguno()
        {
            DashboardService.GetCardDueStatus(MakeCard(null, null), Today).Should().Be("ninguno");
        }

        [Fact]
        public void GetCardDueStatus_CurrentPeriodPaid_ReturnsNinguno()
        {
            var card = MakeCard(Today.AddDays(-5), Today.AddDays(2), isPaid: true);
            DashboardService.GetCardDueStatus(card, Today).Should().Be("ninguno");
        }

        [Fact]
        public void GetCardDueStatus_BeforeClosing_ReturnsNinguno()
        {
            var card = MakeCard(Today.AddDays(3), Today.AddDays(10));
            DashboardService.GetCardDueStatus(card, Today).Should().Be("ninguno");
        }

        [Fact]
        public void GetCardDueStatus_PastDueDate_ReturnsVencido()
        {
            var card = MakeCard(Today.AddDays(-10), Today.AddDays(-1));
            DashboardService.GetCardDueStatus(card, Today).Should().Be("vencido");
        }

        [Fact]
        public void GetCardDueStatus_WithinAlertThreshold_ReturnsAlerta()
        {
            var card = MakeCard(Today.AddDays(-5), Today.AddDays(3));
            DashboardService.GetCardDueStatus(card, Today).Should().Be("alerta");
        }

        [Fact]
        public void GetCardDueStatus_ClosedButFarFromDueDate_ReturnsInformativo()
        {
            var card = MakeCard(Today.AddDays(-1), Today.AddDays(10));
            DashboardService.GetCardDueStatus(card, Today).Should().Be("informativo");
        }

        // ── BuildCardsDue: suma ValueInPesos, toma el vencimiento más próximo ───────────────────

        [Fact]
        public void BuildCardsDue_SumsValueInPesosAndTakesEarliestDueDateOfRelevantCards()
        {
            var summary = new List<CardTransactionPaymentListDTO>
            {
                new() { CardId = 1, ValueInPesos = 1000m },
                new() { CardId = 1, ValueInPesos = 500m },
                new() { CardId = 2, ValueInPesos = 200m }
            };
            var cards = new List<CardDTO>
            {
                MakeCard(Today.AddDays(-5), Today.AddDays(3), id: 1),
                MakeCard(Today.AddDays(-2), Today.AddDays(10), id: 2),
                MakeCard(Today.AddDays(-1), Today.AddDays(1), id: 3) // sin consumo este mes: no cuenta
            };

            var (amount, nextDueDate) = DashboardService.BuildCardsDue(summary, cards);

            amount.Should().Be(1700m);
            nextDueDate.Should().Be(Today.AddDays(3));
        }

        [Fact]
        public void BuildCardsDue_NoSummary_ReturnsZeroAndNullDate()
        {
            var (amount, nextDueDate) = DashboardService.BuildCardsDue(new List<CardTransactionPaymentListDTO>(), new List<CardDTO>());

            amount.Should().Be(0m);
            nextDueDate.Should().BeNull();
        }

        // ── BuildSharedBalances: neto por moneda, filtrando lo que redondea a cero ──────────────

        [Fact]
        public void BuildSharedBalances_NetsPerAssetAndDropsZero()
        {
            var debts = new List<SharedEventConsolidatedDebtDTO>
            {
                new() { AssetId = 1, AssetSymbol = "$", PendingInFavor = 1000m, PendingAgainst = 0m },
                new() { AssetId = 1, AssetSymbol = "$", PendingInFavor = 0m, PendingAgainst = 400m },
                new() { AssetId = 2, AssetSymbol = "US$", PendingInFavor = 50m, PendingAgainst = 50m } // neto cero: se descarta
            };

            var result = DashboardService.BuildSharedBalances(debts);

            result.Should().ContainSingle();
            result[0].AssetId.Should().Be(1);
            result[0].Net.Should().Be(600m);
        }

        // ── GetTripPendingItem: viaje en curso sin gastos recientes ─────────────────────────────

        private static TripDetailDTO MakeTrip(int id, List<TripMovementDTO> movements)
            => new() { Id = id, Name = "Bariloche", Type = "DOMESTIC", StartDate = Today.AddDays(-5), EndDate = Today.AddDays(5), Status = "IN_PROGRESS", Movements = movements };

        [Fact]
        public void GetTripPendingItem_NoMovements_AlwaysReturnsItem()
        {
            var trip = MakeTrip(1, new List<TripMovementDTO>());

            var item = DashboardService.GetTripPendingItem(trip, Today, staleDaysThreshold: 3);

            item.Should().NotBeNull();
            item!.Kind.Should().Be("TripWithoutRecentExpense");
            item.LinkId.Should().Be(1);
        }

        [Fact]
        public void GetTripPendingItem_RecentMovement_ReturnsNull()
        {
            var trip = MakeTrip(1, new List<TripMovementDTO> { new() { Date = Today.AddDays(-1) } });

            var item = DashboardService.GetTripPendingItem(trip, Today, staleDaysThreshold: 3);

            item.Should().BeNull();
        }

        [Fact]
        public void GetTripPendingItem_StaleMovement_ReturnsItemWithDaysSince()
        {
            var trip = MakeTrip(1, new List<TripMovementDTO> { new() { Date = Today.AddDays(-4) } });

            var item = DashboardService.GetTripPendingItem(trip, Today, staleDaysThreshold: 3);

            item.Should().NotBeNull();
            item!.Detail.Should().Be("Sin gastos hace 4 días");
        }

        // ── BuildThermometer: acumulado del mes, mismo tramo del mes anterior, proyección ───────

        [Fact]
        public void BuildThermometer_SumsMonthToDateAndSameStretchOfPreviousMonth()
        {
            // Hoy: 15/09/2026 → 15 días del mes en curso, comparados contra los primeros 15 días de agosto.
            var days = new List<DaySpendingDTO>
            {
                new() { Date = new DateTime(2026, 9, 1), Amount = 100m },
                new() { Date = new DateTime(2026, 9, 10), Amount = 200m },
                new() { Date = new DateTime(2026, 8, 1), Amount = 50m },
                new() { Date = new DateTime(2026, 8, 20), Amount = 999m } // fuera del tramo comparado (día 20 > 15)
            };

            var result = DashboardService.BuildThermometer(days, Today);

            result.MonthToDateAmount.Should().Be(300m);
            result.PreviousMonthSameDayAmount.Should().Be(50m);
            result.DaysElapsed.Should().Be(15);
            result.DaysInMonth.Should().Be(30);
            // Proyección: 300 / 15 * 30
            result.ProjectedMonthEndAmount.Should().Be(600m);
        }

        [Fact]
        public void BuildThermometer_PreviousMonthShorterThanElapsedDays_CapsToItsOwnLength()
        {
            // Hoy: 30/03/2026 → mes anterior es febrero (28 días en 2026, no bisiesto), se compara contra el mes entero.
            var today = new DateTime(2026, 3, 30);
            var days = new List<DaySpendingDTO>
            {
                new() { Date = new DateTime(2026, 2, 28), Amount = 10m }
            };

            var result = DashboardService.BuildThermometer(days, today);

            result.PreviousMonthSameDayAmount.Should().Be(10m);
        }

        // ── GetDashboardAsync: composición con todos los servicios mockeados ───────────────────

        [Fact]
        public async Task GetDashboardAsync_AssetIsNotCurrency_ThrowsBusinessRuleException()
        {
            var stock = new Asset { Id = 5, Name = "AAPL", AssetTypeId = 3 };
            _assetRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(stock);

            var act = async () => await _sut.GetDashboardAsync(UserId, 5);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task GetDashboardAsync_AssetNotFound_ThrowsNotFoundException()
        {
            _assetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Asset)null!);

            var act = async () => await _sut.GetDashboardAsync(UserId, 99);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetDashboardAsync_ComposesIndicatorsFromEachService()
        {
            _assetRepoMock.Setup(r => r.GetByIdAsync(PesoAsset.Id)).ReturnsAsync(PesoAsset);

            _netWorthReportServiceMock.Setup(s => s.GetByAccountAsync(UserId, PesoAsset.Id))
                .ReturnsAsync(new List<AccountBalanceDTO>
                {
                    new()
                    {
                        AccountId = 1,
                        AccountName = "Santander",
                        Holdings = new List<AccountHoldingDTO>
                        {
                            new() { AssetTypeName = "Moneda", BalanceInReferenceAsset = 1000m },
                            new() { AssetTypeName = "Accion Argentina", BalanceInReferenceAsset = 500m } // no es "Disponible"
                        }
                    }
                });
            _netWorthReportServiceMock.Setup(s => s.GetGeneralAsync(UserId))
                .ReturnsAsync(new NetWorthGeneralDTO
                {
                    Totals = new List<NetWorthTotalDTO> { new() { Asset = PesoAsset.Name, GrossBalance = 5000m, NetBalance = 4200m } }
                });
            _netWorthReportServiceMock.Setup(s => s.GetMonthlySeriesAsync(UserId, PesoAsset.Id))
                .ReturnsAsync(new List<NetWorthMonthlyPointDTO>
                {
                    new() { Accounts = 4000m },
                    new() { Accounts = 5000m }
                });

            _incomeExpenseReportServiceMock.Setup(s => s.GetWaterfallAsync(UserId, It.IsAny<DateTime>(), PesoAsset.Id))
                .ReturnsAsync(new IncExpWaterfallDTO { Result = 300m, PreviousMonthResult = 100m });
            _incomeExpenseReportServiceMock.Setup(s => s.GetCalendarAsync(UserId, PesoAsset.Id, It.IsAny<int>()))
                .ReturnsAsync(new SpendingCalendarDTO());

            _cardReportServiceMock.Setup(s => s.GetMonthSummaryAsync(UserId, It.IsAny<DateTime>(), 0))
                .ReturnsAsync(new List<CardTransactionPaymentListDTO>());
            _cardReportServiceMock.Setup(s => s.GetPendingReimbursementsAsync(UserId))
                .ReturnsAsync(new List<PendingReimbursementDTO>());

            _cardServiceMock.Setup(s => s.GetAllForUserAsync(UserId)).ReturnsAsync(new List<CardDTO>());
            _sharedEventServiceMock.Setup(s => s.GetConsolidatedDebtsAsync(UserId)).ReturnsAsync(new List<SharedEventConsolidatedDebtDTO>());
            _sharedEventServiceMock.Setup(s => s.GetActiveSummaryAsync(UserId)).ReturnsAsync(new List<SharedEventActiveSummaryDTO>());
            _tripServiceMock.Setup(s => s.GetAllForUserAsync(UserId)).ReturnsAsync(new List<TripDTO>());

            var result = await _sut.GetDashboardAsync(UserId, PesoAsset.Id);

            result.Indicators.Available.Should().Be(1000m);
            result.Indicators.NetWorthGross.Should().Be(5000m);
            result.Indicators.NetWorthNet.Should().Be(4200m);
            result.Indicators.NetWorthChangeVsPreviousMonth.Should().Be(1000m);
            result.Indicators.MonthResult.Should().Be(300m);
            result.Indicators.MonthResultPreviousMonth.Should().Be(100m);
            result.Pending.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDashboardAsync_IncludesOnlyAlertingCardsAndUnappliedReimbursementsInPending()
        {
            _assetRepoMock.Setup(r => r.GetByIdAsync(PesoAsset.Id)).ReturnsAsync(PesoAsset);
            _netWorthReportServiceMock.Setup(s => s.GetByAccountAsync(UserId, PesoAsset.Id)).ReturnsAsync(new List<AccountBalanceDTO>());
            _netWorthReportServiceMock.Setup(s => s.GetGeneralAsync(UserId)).ReturnsAsync(new NetWorthGeneralDTO());
            _netWorthReportServiceMock.Setup(s => s.GetMonthlySeriesAsync(UserId, PesoAsset.Id)).ReturnsAsync(new List<NetWorthMonthlyPointDTO>());
            _incomeExpenseReportServiceMock.Setup(s => s.GetWaterfallAsync(UserId, It.IsAny<DateTime>(), PesoAsset.Id)).ReturnsAsync(new IncExpWaterfallDTO());
            _incomeExpenseReportServiceMock.Setup(s => s.GetCalendarAsync(UserId, PesoAsset.Id, It.IsAny<int>())).ReturnsAsync(new SpendingCalendarDTO());
            _cardReportServiceMock.Setup(s => s.GetMonthSummaryAsync(UserId, It.IsAny<DateTime>(), 0)).ReturnsAsync(new List<CardTransactionPaymentListDTO>());
            _sharedEventServiceMock.Setup(s => s.GetConsolidatedDebtsAsync(UserId)).ReturnsAsync(new List<SharedEventConsolidatedDebtDTO>());
            _sharedEventServiceMock.Setup(s => s.GetActiveSummaryAsync(UserId)).ReturnsAsync(new List<SharedEventActiveSummaryDTO>());
            _tripServiceMock.Setup(s => s.GetAllForUserAsync(UserId)).ReturnsAsync(new List<TripDTO>());

            // GetDashboardAsync usa DateTime.Today internamente (no el `Today` fijo de arriba, que
            // solo pisan los tests de los métodos puros) — las fechas se arman relativas a hoy para
            // no depender de en qué día corre la suite.
            var realToday = DateTime.Today;
            _cardServiceMock.Setup(s => s.GetAllForUserAsync(UserId)).ReturnsAsync(new List<CardDTO>
            {
                new() { Id = 1, Name = "Visa vencida", NextClosingDate = realToday.AddDays(-10), NextDueDate = realToday.AddDays(-1) },
                new() { Id = 2, Name = "Master al día", NextClosingDate = realToday.AddDays(20), NextDueDate = realToday.AddDays(27) },
                new() { Id = 3, Name = "Visa vence pronto", NextClosingDate = realToday.AddDays(-1), NextDueDate = realToday.AddDays(2) }
            });

            _cardReportServiceMock.Setup(s => s.GetPendingReimbursementsAsync(UserId)).ReturnsAsync(new List<PendingReimbursementDTO>
            {
                new() { CardTransactionId = 10, Detail = "Reintegro Netflix", CardName = "Visa", AssetName = "Dolar Estadounidense", AssetSymbol = "US$", PendingToApply = 150m },
                new() { CardTransactionId = 11, Detail = "Reintegro sin acreditar todavía", CardName = "Visa", AssetName = "Peso Argentino", AssetSymbol = "$", PendingToApply = 0m }
            });

            var result = await _sut.GetDashboardAsync(UserId, PesoAsset.Id);

            result.Pending.Should().HaveCount(3);
            result.Pending.Should().Contain(p => p.Kind == "CardDue" && p.Title == "Visa vencida" && p.Detail == "Vencida" && p.Severity == "danger");
            result.Pending.Should().Contain(p => p.Kind == "CardDue" && p.Title == "Visa vence pronto" && p.Detail == "Vence pronto" && p.Severity == "warning");
            // Corrección 2026-09-08: el monto queda en la moneda nativa del reintegro (US$), no en
            // PesoAsset (la moneda de referencia elegida para el resto del dashboard) — nunca se llama
            // a GetPromotionsAsync (que sí convertiría) para armar este ítem de la bandeja.
            result.Pending.Should().Contain(p => p.Kind == "PendingReimbursement" && p.Amount == 150m && p.AssetSymbol == "US$" && p.Severity == "info");
        }
    }
}
