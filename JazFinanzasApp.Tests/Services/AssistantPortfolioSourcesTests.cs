using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.Assistant.Sources;
using JazFinanzasApp.API.Business.DTO.Dashboard;
using JazFinanzasApp.API.Business.DTO.InvestmentReport;
using JazFinanzasApp.API.Business.DTO.NetWorth;
using JazFinanzasApp.API.Business.DTO.TripReport;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 3 — las fuentes net-worth, investments, trips y alerts.
    internal static class AssistantSourceTestHelpers
    {
        public const int UserId = 1;
        public const int ArsId = 1;
        public const int UsdId = 2;

        public static Mock<IAssistantCurrencyResolver> Currencies()
        {
            var mock = new Mock<IAssistantCurrencyResolver>();
            mock.Setup(c => c.GetAsync()).ReturnsAsync(new AssistantCurrencySet(
                new Asset { Id = ArsId, Name = "Peso Argentino", Symbol = "ARS" },
                new Asset { Id = UsdId, Name = "Dolar Estadounidense", Symbol = "USD" }));
            return mock;
        }

        public static DateTime ThisMonth => new(DateTime.Today.Year, DateTime.Today.Month, 1);
        public static DateTime LastMonth => ThisMonth.AddMonths(-1);

        public static AssistantContextRequest Close(DateTime month) =>
            new(UserId, AssistantReviewKind.MonthClose, month, month.AddMonths(1).AddDays(-1));

        public static AssistantContextRequest Mid(DateTime month, int cutoffDay) =>
            new(UserId, AssistantReviewKind.MidMonth, month, month.AddDays(cutoffDay - 1));
    }

    public class AssistantNetWorthSourceTests
    {
        private static readonly DateTime Aug = new(2026, 8, 1);
        private static readonly DateTime Sep = new(2026, 9, 1);

        private static NetWorthMonthlyPointDTO Point(DateTime month, decimal accounts, decimal stocks, decimal stable, decimal volatile_, decimal bonds) => new()
        {
            Month = month, Accounts = accounts, Stocks = stocks, CryptoStable = stable, CryptoVolatile = volatile_, Bonds = bonds
        };

        private static Dictionary<DateTime, NetWorthMonthlyPointDTO> Index(params NetWorthMonthlyPointDTO[] points) =>
            NetWorthContextSource.IndexByMonth(points);

        [Fact]
        public void TotalChange_ComparesTheMonthEndAgainstThePreviousMonthEnd()
        {
            var points = Index(Point(Aug, 1000, 5000, 100, 400, 500), Point(Sep, 1200, 5500, 100, 300, 600));

            var change = NetWorthContextSource.TotalChange(points, Sep, isPesos: false);

            change.Current.Should().Be(7700m);
            change.Previous.Should().Be(7000m);
            change.Delta.Should().Be(700m);
            change.PctVsPrevious.Should().Be(10.0m);
        }

        [Fact]
        public void TotalChange_WithoutAPreviousMonth_HasNoPercentage()
        {
            var change = NetWorthContextSource.TotalChange(Index(Point(Sep, 1200, 0, 0, 0, 0)), Sep, isPesos: true);

            change.Previous.Should().Be(0m);
            change.PctVsPrevious.Should().BeNull();
        }

        [Fact]
        public void BuildComposition_GroupsStableAndVolatileCryptoTogether()
        {
            var points = Index(Point(Aug, 1000, 5000, 100, 400, 500), Point(Sep, 1200, 5500, 100, 300, 600));

            var c = NetWorthContextSource.BuildComposition(points, Sep, isPesos: false);

            c.Accounts.Delta.Should().Be(200m);
            c.Stocks.PctVsPrevious.Should().Be(10.0m);
            c.Crypto.Current.Should().Be(400m);
            c.Crypto.Previous.Should().Be(500m);
            c.Crypto.Delta.Should().Be(-100m);
            c.Bonds.Delta.Should().Be(100m);
        }

        [Fact]
        public void BuildSeries_ListsTheLastMonthsOldestFirst_AndSkipsTheOnesWithoutData()
        {
            var ars = Index(Point(new DateTime(2026, 6, 1), 600_000, 0, 0, 0, 0), Point(Aug, 800_000, 0, 0, 0, 0), Point(Sep, 900_000, 0, 0, 0, 0));
            var usd = Index(Point(new DateTime(2026, 6, 1), 600, 0, 0, 0, 0), Point(Aug, 800, 0, 0, 0, 0), Point(Sep, 900, 0, 0, 0, 0));

            var series = NetWorthContextSource.BuildSeries(ars, usd, Sep, 6);

            series.Select(s => s.Month).Should().Equal("2026-06", "2026-08", "2026-09");
            series[2].Ars.Should().Be(900_000m);
            series[2].Usd.Should().Be(900m);
        }

        [Fact]
        public async Task BuildAsync_ForAClose_SendsTotalCompositionAndSeriesInBothCurrencies()
        {
            var netWorth = new Mock<INetWorthReportService>();
            netWorth.Setup(n => n.GetMonthlySeriesAsync(AssistantSourceTestHelpers.UserId, AssistantSourceTestHelpers.ArsId))
                .ReturnsAsync(new[] { Point(Aug, 1_000_000, 5_000_000, 0, 0, 0), Point(Sep, 1_200_000, 5_500_000, 0, 0, 0) });
            netWorth.Setup(n => n.GetMonthlySeriesAsync(AssistantSourceTestHelpers.UserId, AssistantSourceTestHelpers.UsdId))
                .ReturnsAsync(new[] { Point(Aug, 1000, 5000, 0, 0, 0), Point(Sep, 1200, 5500, 0, 0, 0) });
            var sut = new NetWorthContextSource(netWorth.Object, AssistantSourceTestHelpers.Currencies().Object);

            var data = (NetWorthContextSource.MonthCloseNetWorthData)(await sut.BuildAsync(AssistantSourceTestHelpers.Close(Sep)))!;

            data.Total.Ars.Current.Should().Be(6_700_000m);
            data.Total.Usd.Current.Should().Be(6700m);
            data.Total.Usd.Delta.Should().Be(700m);
            data.Composition.Usd.Stocks.Delta.Should().Be(500m);
            data.Last6Months.Should().HaveCount(2);
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonth_SendsOnlyTheLatestValueAndItsChange()
        {
            var netWorth = new Mock<INetWorthReportService>();
            netWorth.Setup(n => n.GetMonthlySeriesAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new[] { Point(Aug, 1000, 0, 0, 0, 0), Point(Sep, 1100, 0, 0, 0, 0) });
            var sut = new NetWorthContextSource(netWorth.Object, AssistantSourceTestHelpers.Currencies().Object);

            var data = await sut.BuildAsync(AssistantSourceTestHelpers.Mid(Sep, 16));

            data.Should().BeOfType<NetWorthContextSource.MidMonthNetWorthData>().Which.Total.Usd.Delta.Should().Be(100m);
        }

        [Fact]
        public async Task BuildAsync_WhenTheMonthIsNotInTheSeries_HasNothingToSay()
        {
            var netWorth = new Mock<INetWorthReportService>();
            netWorth.Setup(n => n.GetMonthlySeriesAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new[] { Point(Aug, 1000, 0, 0, 0, 0) });
            var sut = new NetWorthContextSource(netWorth.Object, AssistantSourceTestHelpers.Currencies().Object);

            (await sut.BuildAsync(AssistantSourceTestHelpers.Close(Sep))).Should().BeNull();
        }

        [Fact]
        public void Source_IsPrimarilyInDollars()
        {
            var sut = new NetWorthContextSource(Mock.Of<INetWorthReportService>(), AssistantSourceTestHelpers.Currencies().Object);
            sut.PrimaryCurrency.Should().Be("USD");
            sut.Key.Should().Be("net-worth");
        }
    }

    public class AssistantInvestmentsSourceTests
    {
        private static readonly DateTime Aug = new(2026, 8, 1);
        private static readonly DateTime Sep = new(2026, 9, 1);

        private static InvestmentHoldingDTO Holding(int id, string symbol, string bucket, decimal original, decimal actual, decimal collected = 0m, decimal scale = 1m) => new()
        {
            AssetId = id,
            Symbol = symbol,
            AssetName = symbol,
            Bucket = bucket,
            OriginalValue = original * scale,
            ActualValue = actual * scale,
            CollectedValue = collected * scale,
            GainLossPercent = original != 0 ? Math.Round((actual + collected - original) / original * 100, 2) : null
        };

        private static InvestmentOverviewDTO Overview(decimal scale, params InvestmentHoldingDTO[] holdings)
        {
            var original = holdings.Sum(h => h.OriginalValue);
            var actual = holdings.Sum(h => h.ActualValue);
            var collected = holdings.Sum(h => h.CollectedValue);
            return new InvestmentOverviewDTO
            {
                TotalOriginalValue = original,
                TotalActualValue = actual,
                TotalCollectedValue = collected,
                GainLossPercent = original != 0 ? Math.Round((actual + collected - original) / original * 100, 2) : null,
                Holdings = holdings.ToList(),
                ValueSeries = new List<InvestmentValuePointDTO>
                {
                    new() { Month = Aug, Value = 10_000m * scale },
                    new() { Month = Sep, Value = 11_500m * scale }
                },
                ContributionMarkers = new List<InvestmentContributionMarkerDTO>
                {
                    new() { Month = Aug, Contributed = 5_000m * scale, Withdrawn = 0m },
                    new() { Month = Sep, Contributed = 1_000m * scale, Withdrawn = 200m * scale }
                }
            };
        }

        private static InvestmentOverviewDTO Sample(decimal scale) => Overview(scale,
            Holding(1, "AAPL", "Bolsa", 5000, 6000, scale: scale),
            Holding(2, "KO", "Bolsa", 2500, 2000, scale: scale),
            Holding(3, "BTC", "Cripto", 500, 1000, scale: scale),
            Holding(4, "AL30", "Bonos", 900, 1000, 100, scale: scale));

        // ── Pura ──────────────────────────────────────────────────────────────────────────────

        [Fact]
        public void BuildMonthFlow_SeparatesWhatWasContributedFromWhatTheMarketDid()
        {
            var flow = InvestmentsContextSource.BuildMonthFlow(Sample(1m), Sep, isPesos: false)!;

            flow.ValueAtMonthEnd.Should().Be(11_500m);
            flow.ValueAtPreviousMonthEnd.Should().Be(10_000m);
            flow.Contributed.Should().Be(1_000m);
            flow.Withdrawn.Should().Be(200m);
            flow.MarketResult.Should().Be(700m);       // 11.500 - 10.000 - 1.000 + 200
            flow.MarketResultPct.Should().Be(7.0m);
        }

        [Fact]
        public void BuildMonthFlow_WhenTheSeriesLacksAMonth_IsNull()
        {
            InvestmentsContextSource.BuildMonthFlow(Sample(1m), new DateTime(2026, 11, 1), isPesos: false).Should().BeNull();
            InvestmentsContextSource.BuildMonthFlow(Sample(1m), Aug, isPesos: false).Should().BeNull(); // falta julio
        }

        [Fact]
        public void BuildPortfolio_KeepsTheLargestPositionsInDollarsAndGroupsTheRest()
        {
            var portfolio = InvestmentsContextSource.BuildPortfolio(Sample(1000m), Sample(1m), maxHoldings: 3);

            // AL30 y BTC valen lo mismo en dólares: desempata el símbolo.
            portfolio.TopHoldings.Select(h => h.Symbol).Should().Equal("AAPL", "KO", "AL30");
            portfolio.TopHoldings[0].Figures.Usd.Actual.Should().Be(6000m);
            portfolio.TopHoldings[0].Figures.Ars.Actual.Should().Be(6_000_000m);
            portfolio.TopHoldings[0].ShareOfPortfolioPct.Should().Be(60.0m);   // 6.000 de 10.000
            portfolio.Others.Should().NotBeNull();
            portfolio.Others!.HoldingsCount.Should().Be(1);
            portfolio.Others.Actual.Usd.Should().Be(1000m);
            (portfolio.TopHoldings.Sum(h => h.Figures.Usd.Actual) + portfolio.Others.Actual.Usd).Should().Be(portfolio.Totals.Usd.Actual);
        }

        [Fact]
        public void BuildPortfolio_ComputesTotalsAndGainOrLossByBucket()
        {
            var portfolio = InvestmentsContextSource.BuildPortfolio(Sample(1000m), Sample(1m), maxHoldings: 8);

            portfolio.Totals.Usd.Actual.Should().Be(10_000m);
            portfolio.Totals.Usd.Original.Should().Be(8_900m);
            portfolio.Totals.Usd.Collected.Should().Be(100m);
            portfolio.Totals.Usd.GainLossPct.Should().Be(13.5m);   // (10.000 + 100 - 8.900) / 8.900

            portfolio.Buckets.Select(b => b.Bucket).Should().Equal("Bolsa", "Bonos", "Cripto");
            var bolsa = portfolio.Buckets[0];
            bolsa.Figures.Usd.Actual.Should().Be(8000m);
            bolsa.Figures.Usd.GainLossPct.Should().Be(6.7m);        // (8.000 - 7.500) / 7.500
            bolsa.ShareOfPortfolioPct.Should().Be(80.0m);
            var bonos = portfolio.Buckets[1];
            bonos.Figures.Usd.GainLossPct.Should().Be(22.2m);       // lo cobrado del bono suma al resultado
            portfolio.Others.Should().BeNull();
        }

        [Fact]
        public void BuildPortfolio_LeavesOutClosedPositions()
        {
            var open = Holding(1, "AAPL", "Bolsa", 5000, 6000);
            var closed = Holding(2, "OLD", "Bolsa", 100, 0);

            var portfolio = InvestmentsContextSource.BuildPortfolio(Overview(1000m, open, closed), Overview(1m, open, closed), 8);

            portfolio.TopHoldings.Select(h => h.Symbol).Should().Equal("AAPL");
        }

        [Fact]
        public void SelectBondPayments_KeepsTheWindow_RoundsByTheCurrencyOfEachBond_AndKeepsIndexedWithoutAmount()
        {
            var report = new BondUpcomingReportDTO
            {
                Payments = new List<BondUpcomingPaymentDTO>
                {
                    new() { PaymentDate = new DateTime(2026, 10, 9), Symbol = "AL30", CurrencySymbol = "USD", EstimatedCapital = 10.456m, EstimatedInterest = 1.234m, EstimatedTotal = 11.69m },
                    new() { PaymentDate = new DateTime(2026, 10, 5), Symbol = "TX26", CurrencySymbol = "ARS", IsIndexed = true },
                    new() { PaymentDate = new DateTime(2026, 11, 1), Symbol = "FUERA", CurrencySymbol = "USD", EstimatedTotal = 1m },
                    new() { PaymentDate = new DateTime(2026, 9, 30), Symbol = "ANTES", CurrencySymbol = "USD", EstimatedTotal = 1m },
                    new() { PaymentDate = new DateTime(2026, 10, 20), Symbol = "BONAR", CurrencySymbol = "ARS", EstimatedTotal = 1234.6m }
                }
            };

            var result = InvestmentsContextSource.SelectBondPayments(report, new DateTime(2026, 10, 1), new DateTime(2026, 11, 1));

            result.Select(p => p.Symbol).Should().Equal("TX26", "AL30", "BONAR");
            result[0].Total.Should().BeNull();
            result[0].IsIndexed.Should().BeTrue();
            result[1].Capital.Should().Be(10.46m);
            result[1].Date.Should().Be("2026-10-09");
            result[2].Total.Should().Be(1235m);
        }

        // ── BuildAsync ────────────────────────────────────────────────────────────────────────

        private static (InvestmentsContextSource Sut, Mock<IInvestmentReportService> Reports) Create(DateTime month)
        {
            var reports = new Mock<IInvestmentReportService>();
            reports.Setup(r => r.GetBondUpcomingAsync(AssistantSourceTestHelpers.UserId)).ReturnsAsync(new BondUpcomingReportDTO());

            // La serie de prueba usa dos meses consecutivos que terminan en `month`.
            InvestmentOverviewDTO Shifted(decimal scale)
            {
                var o = Sample(scale);
                o.ValueSeries[0].Month = month.AddMonths(-1);
                o.ValueSeries[1].Month = month;
                o.ContributionMarkers[0].Month = month.AddMonths(-1);
                o.ContributionMarkers[1].Month = month;
                return o;
            }

            reports.Setup(r => r.GetOverviewAsync(AssistantSourceTestHelpers.UserId, AssistantSourceTestHelpers.ArsId)).ReturnsAsync(Shifted(1000m));
            reports.Setup(r => r.GetOverviewAsync(AssistantSourceTestHelpers.UserId, AssistantSourceTestHelpers.UsdId)).ReturnsAsync(Shifted(1m));
            return (new InvestmentsContextSource(reports.Object, AssistantSourceTestHelpers.Currencies().Object), reports);
        }

        [Fact]
        public async Task BuildAsync_ForTheLastClosedMonth_SendsThePortfolioAndTheMonthFlow()
        {
            var (sut, _) = Create(AssistantSourceTestHelpers.LastMonth);

            var data = (InvestmentsContextSource.MonthCloseInvestmentsData)(await sut.BuildAsync(AssistantSourceTestHelpers.Close(AssistantSourceTestHelpers.LastMonth)))!;

            data.PortfolioToday.Should().NotBeNull();
            data.MonthFlow!.Usd.MarketResult.Should().Be(700m);
            data.MonthFlow.Ars.MarketResult.Should().Be(700_000m);
        }

        [Fact]
        public async Task BuildAsync_ForAnOldClose_DoesNotSendTheCurrentPortfolio_ButKeepsTheMonthFlow()
        {
            var old = AssistantSourceTestHelpers.ThisMonth.AddMonths(-4);
            var (sut, _) = Create(old);

            var data = (InvestmentsContextSource.MonthCloseInvestmentsData)(await sut.BuildAsync(AssistantSourceTestHelpers.Close(old)))!;

            data.PortfolioToday.Should().BeNull();
            data.MonthFlow.Should().NotBeNull();
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonth_SendsOnlyBondPayments_WithoutAskingForTheOverview()
        {
            var month = AssistantSourceTestHelpers.ThisMonth;
            var (sut, reports) = Create(month);
            reports.Setup(r => r.GetBondUpcomingAsync(AssistantSourceTestHelpers.UserId)).ReturnsAsync(new BondUpcomingReportDTO
            {
                Payments = new List<BondUpcomingPaymentDTO>
                {
                    new() { PaymentDate = month.AddDays(20), Symbol = "AL30", CurrencySymbol = "USD", EstimatedTotal = 11.69m },
                    new() { PaymentDate = month.AddDays(3), Symbol = "YA_PASO", CurrencySymbol = "USD", EstimatedTotal = 5m }
                }
            });

            var data = (InvestmentsContextSource.MidMonthInvestmentsData)(await sut.BuildAsync(AssistantSourceTestHelpers.Mid(month, 16)))!;

            data.UpcomingBondPayments.Select(p => p.Symbol).Should().Equal("AL30");
            reports.Verify(r => r.GetOverviewAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonthWithoutPayments_HasNothingToSay()
        {
            var (sut, _) = Create(AssistantSourceTestHelpers.ThisMonth);

            (await sut.BuildAsync(AssistantSourceTestHelpers.Mid(AssistantSourceTestHelpers.ThisMonth, 16))).Should().BeNull();
        }

        [Fact]
        public async Task BuildAsync_ForAClose_LooksForBondPaymentsInTheFollowingMonth()
        {
            var month = AssistantSourceTestHelpers.LastMonth;
            var (sut, reports) = Create(month);
            reports.Setup(r => r.GetBondUpcomingAsync(AssistantSourceTestHelpers.UserId)).ReturnsAsync(new BondUpcomingReportDTO
            {
                Payments = new List<BondUpcomingPaymentDTO>
                {
                    new() { PaymentDate = month.AddMonths(1).AddDays(8), Symbol = "AL30", CurrencySymbol = "USD", EstimatedTotal = 11.69m },
                    new() { PaymentDate = month.AddMonths(2).AddDays(1), Symbol = "OTRO_MES", CurrencySymbol = "USD", EstimatedTotal = 3m }
                }
            });

            var data = (InvestmentsContextSource.MonthCloseInvestmentsData)(await sut.BuildAsync(AssistantSourceTestHelpers.Close(month)))!;

            data.UpcomingBondPayments.Select(p => p.Symbol).Should().Equal("AL30");
        }

        [Fact]
        public void Measures_ExplainsThatBondCollectionsCountAsWithdrawals()
        {
            var sut = new InvestmentsContextSource(Mock.Of<IInvestmentReportService>(), AssistantSourceTestHelpers.Currencies().Object);
            sut.Measures.Should().Contain("cobros de bonos cuentan como retiros");
            sut.PrimaryCurrency.Should().Be("USD");
        }
    }

    public class AssistantTripsSourceTests
    {
        private static TripGeneralReportDTO Trip(int id, string name, DateTime start, DateTime end, decimal total, decimal perDay) => new()
        {
            TripId = id, Name = name, Type = "Vacaciones", Status = "Finalizado", StartDate = start, EndDate = end,
            DurationDays = (int)(end - start).TotalDays + 1, Total = total, CostPerDay = perDay
        };

        private static TripDetailReportDTO Detail(params (DateTime Date, decimal Amount)[] days) => new()
        {
            DailySpending = days.Select(d => new TripDailySpendingDTO { Date = d.Date, Amount = d.Amount }).ToArray()
        };

        [Theory]
        [InlineData("2026-09-10", "2026-09-20", true)]    // adentro
        [InlineData("2026-08-25", "2026-09-02", true)]    // arranca antes, termina adentro
        [InlineData("2026-09-28", "2026-10-05", true)]    // arranca adentro, termina después
        [InlineData("2026-08-01", "2026-08-31", false)]   // termina el día anterior
        [InlineData("2026-10-01", "2026-10-10", false)]   // arranca el día siguiente
        [InlineData("2026-08-20", "2026-09-01", true)]    // termina el primer día del período
        public void OverlapsPeriod_ChecksBothEnds(string start, string end, bool expected)
        {
            var trip = Trip(1, "Viaje", DateTime.Parse(start), DateTime.Parse(end), 0, 0);

            TripsContextSource.OverlapsPeriod(trip, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)).Should().Be(expected);
        }

        [Fact]
        public void SpentInPeriod_AddsOnlyTheDaysInsideTheRange()
        {
            var detail = Detail((new DateTime(2026, 8, 31), 999m), (new DateTime(2026, 9, 1), 100m), (new DateTime(2026, 9, 15), 50m), (new DateTime(2026, 10, 1), 999m));

            TripsContextSource.SpentInPeriod(detail, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)).Should().Be(150m);
        }

        [Fact]
        public async Task BuildAsync_SendsTheTripsThatTouchThePeriod_WithTheirTotalsAndTheSpendInThePeriod()
        {
            var trips = new Mock<ITripReportService>();
            trips.Setup(t => t.GetGeneralAsync(AssistantSourceTestHelpers.UserId, AssistantSourceTestHelpers.ArsId)).ReturnsAsync(new[]
            {
                Trip(1, "Bariloche", new DateTime(2026, 9, 10), new DateTime(2026, 9, 17), 800_000m, 100_000m),
                Trip(2, "Mendoza", new DateTime(2026, 3, 1), new DateTime(2026, 3, 5), 300_000m, 60_000m)
            });
            trips.Setup(t => t.GetGeneralAsync(AssistantSourceTestHelpers.UserId, AssistantSourceTestHelpers.UsdId)).ReturnsAsync(new[]
            {
                Trip(1, "Bariloche", new DateTime(2026, 9, 10), new DateTime(2026, 9, 17), 800m, 100m),
                Trip(2, "Mendoza", new DateTime(2026, 3, 1), new DateTime(2026, 3, 5), 300m, 60m)
            });
            trips.Setup(t => t.GetDetailAsync(AssistantSourceTestHelpers.UserId, 1, AssistantSourceTestHelpers.ArsId))
                .ReturnsAsync(Detail((new DateTime(2026, 9, 10), 500_000m), (new DateTime(2026, 9, 12), 250_000m)));
            trips.Setup(t => t.GetDetailAsync(AssistantSourceTestHelpers.UserId, 1, AssistantSourceTestHelpers.UsdId))
                .ReturnsAsync(Detail((new DateTime(2026, 9, 10), 500m), (new DateTime(2026, 9, 12), 250m)));
            var sut = new TripsContextSource(trips.Object, AssistantSourceTestHelpers.Currencies().Object);

            var data = (TripsContextSource.TripsData)(await sut.BuildAsync(AssistantSourceTestHelpers.Close(new DateTime(2026, 9, 1))))!;

            var trip = data.Trips.Should().ContainSingle().Subject;
            trip.Name.Should().Be("Bariloche");
            trip.StartDate.Should().Be("2026-09-10");
            trip.Total.Ars.Should().Be(800_000m);
            trip.Total.Usd.Should().Be(800m);
            trip.CostPerDay.Usd.Should().Be(100m);
            trip.SpentInPeriod.Ars.Should().Be(750_000m);
            trip.SpentInPeriod.Usd.Should().Be(750m);
            trips.Verify(t => t.GetDetailAsync(It.IsAny<int>(), 2, It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonth_CutsThePeriodAtTheCutoffDay()
        {
            var trips = new Mock<ITripReportService>();
            var general = new[] { Trip(1, "Bariloche", new DateTime(2026, 9, 20), new DateTime(2026, 9, 27), 800m, 100m) };
            trips.Setup(t => t.GetGeneralAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(general);
            trips.Setup(t => t.GetDetailAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Detail());
            var sut = new TripsContextSource(trips.Object, AssistantSourceTestHelpers.Currencies().Object);

            // Al 16 de septiembre el viaje del 20 todavía no empezó.
            (await sut.BuildAsync(AssistantSourceTestHelpers.Mid(new DateTime(2026, 9, 1), 16))).Should().BeNull();
        }
    }

    public class AssistantAlertsSourceTests
    {
        private static DashboardPendingItemDTO Item(string kind, string title, string? detail = null, decimal? amount = null, string? symbol = null, DateTime? date = null, string severity = "info") =>
            new() { Kind = kind, Title = title, Detail = detail, Amount = amount, AssetSymbol = symbol, Date = date, Severity = severity, LinkId = 7 };

        private static Mock<IDashboardService> Dashboard(params DashboardPendingItemDTO[] pending)
        {
            var mock = new Mock<IDashboardService>();
            mock.Setup(d => d.GetDashboardAsync(AssistantSourceTestHelpers.UserId, It.IsAny<int>()))
                .ReturnsAsync(new DashboardDTO { Pending = pending.ToList() });
            return mock;
        }

        [Fact]
        public void ToEntry_KeepsWhatTheUserSees_AndDropsTheInternalLink()
        {
            var entry = AlertsContextSource.ToEntry(Item("CardDue", "Visa", "Vence pronto", date: new DateTime(2026, 9, 25), severity: "warning"));

            entry.Should().Be(new AlertsContextSource.PendingEntry("CardDue", "Visa", "Vence pronto", null, null, "2026-09-25", "warning"));
        }

        [Fact]
        public void ToEntry_RoundsTheAmountByTheCurrencyOfTheAlert()
        {
            AlertsContextSource.ToEntry(Item("OpenSharedEvent", "Asado", amount: 1234.6m, symbol: "ARS")).Amount.Should().Be(1235m);
            AlertsContextSource.ToEntry(Item("OpenSharedEvent", "Asado", amount: 12.345m, symbol: "USD")).Amount.Should().Be(12.35m);
        }

        [Fact]
        public async Task BuildAsync_ForAMidMonth_SendsTodaysPendings()
        {
            var sut = new AlertsContextSource(Dashboard(Item("CardDue", "Visa"), Item("StaleQuote", "Bonos")).Object, AssistantSourceTestHelpers.Currencies().Object);

            var data = (AlertsContextSource.AlertsData)(await sut.BuildAsync(AssistantSourceTestHelpers.Mid(AssistantSourceTestHelpers.ThisMonth, 16)))!;

            data.AsOf.Should().Be(DateTime.Today.ToString("yyyy-MM-dd"));
            data.Pending.Select(p => p.Kind).Should().Equal("CardDue", "StaleQuote");
        }

        [Fact]
        public async Task BuildAsync_ForTheLastClosedMonth_SendsTodaysPendings()
        {
            var sut = new AlertsContextSource(Dashboard(Item("CardDue", "Visa")).Object, AssistantSourceTestHelpers.Currencies().Object);

            (await sut.BuildAsync(AssistantSourceTestHelpers.Close(AssistantSourceTestHelpers.LastMonth))).Should().NotBeNull();
        }

        [Fact]
        public async Task BuildAsync_ForAnOldClose_SendsNothing_BecauseTheTrayIsTodaysState()
        {
            var dashboard = Dashboard(Item("CardDue", "Visa"));
            var sut = new AlertsContextSource(dashboard.Object, AssistantSourceTestHelpers.Currencies().Object);

            var data = await sut.BuildAsync(AssistantSourceTestHelpers.Close(AssistantSourceTestHelpers.ThisMonth.AddMonths(-4)));

            data.Should().BeNull();
            dashboard.Verify(d => d.GetDashboardAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task BuildAsync_WithAnEmptyTray_HasNothingToSay()
        {
            var sut = new AlertsContextSource(Dashboard().Object, AssistantSourceTestHelpers.Currencies().Object);

            (await sut.BuildAsync(AssistantSourceTestHelpers.Mid(AssistantSourceTestHelpers.ThisMonth, 16))).Should().BeNull();
        }

        [Fact]
        public void Measures_SaysTheTrayIsTodaysState()
        {
            var sut = new AlertsContextSource(Mock.Of<IDashboardService>(), AssistantSourceTestHelpers.Currencies().Object);
            sut.Measures.Should().Contain("al día de hoy");
        }
    }

    public class AssistantContextIsLiveTests
    {
        [Fact]
        public void IsLive_IsTrueForTheCurrentAndThePreviousMonth_AndFalseBeforeThat()
        {
            var thisMonth = AssistantSourceTestHelpers.ThisMonth;

            AssistantSourceTestHelpers.Close(thisMonth).IsLive.Should().BeTrue();
            AssistantSourceTestHelpers.Close(thisMonth.AddMonths(-1)).IsLive.Should().BeTrue();
            AssistantSourceTestHelpers.Close(thisMonth.AddMonths(-2)).IsLive.Should().BeFalse();
        }
    }
}
