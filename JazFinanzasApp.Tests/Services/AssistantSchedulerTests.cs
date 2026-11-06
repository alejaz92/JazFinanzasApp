using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.DTO.Assistant;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm-v2.md, Fase 7 — la preparación de madrugada (D6) y lo que informa reviews/latest (D7). Con el cliente
    // falso del v1: ningún test llama a la API real. El "hoy" de la fixture es el sábado 17 de octubre de 2026.
    public class AssistantSchedulerTests
    {
        private const int UserId = AssistantReviewServiceTests.UserId;

        private static Mock<IAssistantMonthReadinessService> Readiness(bool ready = true, bool deadlineReached = false, params string[] missing)
        {
            var mock = new Mock<IAssistantMonthReadinessService>();
            mock.Setup(r => r.CheckAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new AssistantMonthReadiness(ready, missing, deadlineReached));
            return mock;
        }

        private static (AssistantReviewServiceTests.Fixture Fixture, AssistantSchedulerService Sut, AssistantSchedulerRunRepository Runs) Build(
            Mock<IAssistantMonthReadinessService>? readiness = null, AssistantSchedulerOptions? options = null, bool enabled = true, decimal budget = 3m)
        {
            readiness ??= Readiness();
            var f = new AssistantReviewServiceTests.Fixture(enabled: enabled, budget: budget, readiness: readiness.Object);
            var runs = new AssistantSchedulerRunRepository(f.Context);
            var sut = new AssistantSchedulerService(
                new AssistantUsageRepository(f.Context), f.Usage, f.Service, readiness.Object, runs,
                Options.Create(options ?? new AssistantSchedulerOptions()), f.Time, NullLogger<AssistantSchedulerService>.Instance);
            return (f, sut, runs);
        }

        private static void AddActivityInBothMonths(AssistantReviewServiceTests.Fixture f)
        {
            f.AddActivityInSeptember();
            f.AddActivityInOctober();
        }

        [Fact]
        public async Task PreparesTheCloseAndTheMidMonthOfAnEnabledUserAndRecordsTheRun()
        {
            var (f, sut, _) = Build();
            AddActivityInBothMonths(f);

            var result = await sut.RunAsync();

            result.Status.Should().Be(AssistantSchedulerRunStatus.Completed);
            result.Prepared.Should().Be(2);
            result.Failed.Should().Be(0);
            f.Client.Requests.Should().HaveCount(2);
            f.Context.AssistantReviews.Where(r => r.Status == AssistantReviewStatus.Ready).Select(r => r.Kind)
                .Should().BeEquivalentTo(new[] { AssistantReviewKind.MonthClose, AssistantReviewKind.MidMonth });

            var run = f.Context.AssistantSchedulerRuns.Single();
            run.Status.Should().Be(AssistantSchedulerRunStatus.Completed);
            run.Prepared.Should().Be(2);
            run.FinishedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ASecondRunHasNothingLeftToDo()
        {
            var (f, sut, _) = Build();
            AddActivityInBothMonths(f);
            await sut.RunAsync();
            f.Client.Requests.Clear();

            var result = await sut.RunAsync();

            result.Prepared.Should().Be(0);
            f.Client.Requests.Should().BeEmpty();
            f.Context.AssistantSchedulerRuns.Should().HaveCount(2);
        }

        [Fact]
        public async Task AUserWithoutTheAssistantEnabledIsNotProcessed()
        {
            var (f, sut, _) = Build(enabled: false);
            AddActivityInBothMonths(f);

            var result = await sut.RunAsync();

            result.Prepared.Should().Be(0);
            f.Client.Requests.Should().BeEmpty();
            f.Context.AssistantReviews.Should().BeEmpty();
        }

        [Fact]
        public async Task AUserWhoReachedTheSpendingCapIsSkippedWithoutCallingTheModel()
        {
            var (f, sut, _) = Build(budget: 0m);
            AddActivityInBothMonths(f);

            var result = await sut.RunAsync();

            result.Skipped.Should().Be(1);
            result.Notes.Should().Contain("tope");
            f.Client.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task TheCloseWaitsForTheMonthToBeReady_AndTheMidMonthStillGoesOut()
        {
            // Faltan datos y todavía no llegó el tope del día 7: el cierre y la revisión semanal esperan.
            var (f, sut, _) = Build(Readiness(ready: false, deadlineReached: false, "Falta la cotización del 30/09 de: GGAL."));
            AddActivityInBothMonths(f);

            var result = await sut.RunAsync();

            result.Prepared.Should().Be(1);
            result.Skipped.Should().Be(1);
            result.Notes.Should().Contain("espera datos").And.Contain("GGAL");
            f.Context.AssistantReviews.Single().Kind.Should().Be(AssistantReviewKind.MidMonth);
        }

        [Fact]
        public async Task TheCloseIsPreparedAnywayOnceTheDeadlineIsReached()
        {
            var (f, sut, _) = Build(Readiness(ready: false, deadlineReached: true, "Falta la cotización del 30/09 de: GGAL."));
            AddActivityInBothMonths(f);

            var result = await sut.RunAsync();

            result.Prepared.Should().Be(2);
            f.Context.AssistantReviews.Should().Contain(r => r.Kind == AssistantReviewKind.MonthClose && r.Status == AssistantReviewStatus.Ready);
        }

        [Fact]
        public async Task ItDoesNotStartAPassThatDoesNotFitInTheTimeBudget_AndLeavesTheRestForTheNextRun()
        {
            var options = new AssistantSchedulerOptions { TimeBudget = TimeSpan.FromMinutes(5), EstimatedPassDuration = TimeSpan.FromMinutes(2.5) };
            var (f, sut, _) = Build(options: options);
            AddActivityInBothMonths(f);
            // Cada pasada "tarda" 3 minutos: después de la primera ya no entra otra.
            f.Client.Respond = () =>
            {
                f.Time.Now = f.Time.Now.AddMinutes(3);
                return AssistantReviewServiceTests.Ok(AssistantReviewServiceTests.GoodOutput);
            };

            var result = await sut.RunAsync();

            result.Status.Should().Be(AssistantSchedulerRunStatus.Partial);
            result.Prepared.Should().Be(1);
            result.Skipped.Should().BeGreaterThan(0);
            result.Notes.Should().Contain("sin tiempo");
            f.Client.Requests.Should().HaveCount(1);

            // La corrida siguiente retoma lo que quedó.
            f.Client.Respond = () => AssistantReviewServiceTests.Ok(AssistantReviewServiceTests.GoodOutput);
            f.Time.Now = f.Time.Now.AddHours(1);
            var next = await sut.RunAsync();

            next.Prepared.Should().Be(1);
            f.Context.AssistantReviews.Count(r => r.Status == AssistantReviewStatus.Ready).Should().Be(2);
        }

        [Fact]
        public async Task AFailedPassIsCountedAndTheRunIsMarkedFailedWhenNothingWasPrepared()
        {
            var (f, sut, _) = Build();
            AddActivityInBothMonths(f);
            f.Client.Throw = new HttpRequestException("sin conexión");

            var result = await sut.RunAsync();

            result.Status.Should().Be(AssistantSchedulerRunStatus.Failed);
            result.Failed.Should().BeGreaterThan(0);
            result.Prepared.Should().Be(0);
            f.Context.AssistantSchedulerRuns.Single().Status.Should().Be(AssistantSchedulerRunStatus.Failed);
        }

        [Fact]
        public async Task ARunDoesNothingWhileAnotherOneIsInProgress()
        {
            var (f, sut, runs) = Build();
            AddActivityInBothMonths(f);
            await runs.AddAsync(new AssistantSchedulerRun { StartedAt = f.Time.Now.AddMinutes(-5), Status = AssistantSchedulerRunStatus.Running });

            var result = await sut.RunAsync();

            result.Status.Should().Be(AssistantSchedulerRunResultStatus.AlreadyRunning);
            f.Client.Requests.Should().BeEmpty();
            f.Context.AssistantSchedulerRuns.Should().HaveCount(1);
        }

        [Fact]
        public async Task AnOldRunningRunDoesNotBlockTheNextOne()
        {
            var (f, sut, runs) = Build();
            AddActivityInBothMonths(f);
            await runs.AddAsync(new AssistantSchedulerRun { StartedAt = f.Time.Now.AddHours(-3), Status = AssistantSchedulerRunStatus.Running });

            var result = await sut.RunAsync();

            result.Status.Should().Be(AssistantSchedulerRunStatus.Completed);
        }

        // ── Qué pasadas tocan ────────────────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task BeforeDay15OnlyTheCloseIsDue()
        {
            var (f, _, _) = Build();
            f.Time.Now = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
            f.AddActivityInSeptember();

            var due = await f.Service.GetDuePassesAsync(UserId);

            due.Select(d => d.Kind).Should().Equal(AssistantReviewKind.MonthClose);
            due[0].PeriodStart.Should().Be(new DateTime(2026, 9, 1));
        }

        [Fact]
        public async Task FromDay15TheMidMonthIsAlsoDue()
        {
            var (f, _, _) = Build();

            var due = await f.Service.GetDuePassesAsync(UserId);

            due.Select(d => d.Kind).Should().Equal(AssistantReviewKind.MonthClose, AssistantReviewKind.MidMonth);
        }

        [Fact]
        public async Task TheWeeklyScanIsDueOnlyWhenNoDevolutionIsMissing()
        {
            var (f, sut, _) = Build();
            AddActivityInBothMonths(f);
            (await f.Service.GetDuePassesAsync(UserId)).Should().NotContain(d => d.Kind == AssistantReviewKind.WeeklyScan);

            await sut.RunAsync();
            // Un movimiento cargado después de la última pasada, y otro día de la semana.
            f.Time.Now = f.Time.Now.AddMinutes(30);
            f.AddActivity(new DateTime(2026, 10, 16), loadedAt: f.Time.Now.AddMinutes(10));
            f.Time.Now = f.Time.Now.AddDays(3);

            var due = await f.Service.GetDuePassesAsync(UserId);

            due.Should().NotContain(d => d.Kind != AssistantReviewKind.WeeklyScan);
        }

        [Fact]
        public async Task GettingTheDuePassesRequiresTheAssistantToBeEnabled()
        {
            var (f, _, _) = Build(enabled: false);

            await FluentActions.Awaiting(() => f.Service.GetDuePassesAsync(UserId)).Should().ThrowAsync<JazFinanzasApp.API.Business.Exceptions.BusinessRuleException>();
        }
    }

    public class AssistantLatestOutlookTests
    {
        private const int UserId = AssistantReviewServiceTests.UserId;

        private static AssistantReviewServiceTests.Fixture Build(Mock<IAssistantMonthReadinessService>? readiness = null)
        {
            var f = new AssistantReviewServiceTests.Fixture(readiness: readiness?.Object, runs: null);
            return f;
        }

        private static (AssistantReviewServiceTests.Fixture Fixture, AssistantSchedulerRunRepository Runs) BuildWithRuns(Mock<IAssistantMonthReadinessService>? readiness = null)
        {
            // La fixture necesita el repositorio de corridas al construirse; se arma contra su misma base.
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options;
            var runsContext = new ApplicationDbContext(options);
            var runs = new AssistantSchedulerRunRepository(runsContext);
            var f = new AssistantReviewServiceTests.Fixture(dbName: dbName, readiness: readiness?.Object, runs: runs);
            return (f, runs);
        }

        private static Mock<IAssistantMonthReadinessService> NotReady(params string[] missing)
        {
            var mock = new Mock<IAssistantMonthReadinessService>();
            mock.Setup(r => r.CheckAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new AssistantMonthReadiness(false, missing, false));
            return mock;
        }

        [Fact]
        public async Task WhenTheCloseIsMissingItStaysNotGeneratedAndSaysItIsScheduled()
        {
            var (f, runs) = BuildWithRuns();
            await runs.AddAsync(new AssistantSchedulerRun { StartedAt = f.Time.Now.AddHours(-10), Status = AssistantSchedulerRunStatus.Completed });

            var latest = await f.Service.GetLatestAsync(UserId);

            latest.State.Should().Be(AssistantLatestState.NotGenerated);
            latest.MissingKind.Should().Be(AssistantReviewKind.MonthClose);
            latest.Outlook.Should().Be(AssistantLatestOutlook.Scheduled);
        }

        [Fact]
        public async Task WithoutARecentRunItSaysTheSchedulerDidNotRun()
        {
            var (f, runs) = BuildWithRuns();
            await runs.AddAsync(new AssistantSchedulerRun { StartedAt = f.Time.Now.AddDays(-5), Status = AssistantSchedulerRunStatus.Completed });

            (await f.Service.GetLatestAsync(UserId)).Outlook.Should().Be(AssistantLatestOutlook.SchedulerNotRun);
        }

        [Fact]
        public async Task WithNoRunsAtAllItSaysTheSchedulerDidNotRun()
        {
            var (f, _) = BuildWithRuns();

            (await f.Service.GetLatestAsync(UserId)).Outlook.Should().Be(AssistantLatestOutlook.SchedulerNotRun);
        }

        [Fact]
        public async Task WhenTheLastRunFailedItSaysSo()
        {
            var (f, runs) = BuildWithRuns();
            await runs.AddAsync(new AssistantSchedulerRun { StartedAt = f.Time.Now.AddHours(-10), Status = AssistantSchedulerRunStatus.Failed });

            (await f.Service.GetLatestAsync(UserId)).Outlook.Should().Be(AssistantLatestOutlook.LastRunFailed);
        }

        [Fact]
        public async Task ACloseWaitingForDataSaysWhatIsMissing()
        {
            var (f, _) = BuildWithRuns(NotReady("Falta la cotización del 30/09 de: GGAL."));

            var latest = await f.Service.GetLatestAsync(UserId);

            latest.Outlook.Should().Be(AssistantLatestOutlook.WaitingForData);
            latest.OutlookNotes.Should().Equal("Falta la cotización del 30/09 de: GGAL.");
        }

        [Fact]
        public async Task ARunningPassIsReportedAsPreparing()
        {
            var f = Build();
            f.Context.AssistantReviews.Add(new AssistantReview
            {
                UserId = UserId, Kind = AssistantReviewKind.MonthClose, PeriodStart = new DateTime(2026, 9, 1), CutoffDate = new DateTime(2026, 9, 30),
                Status = AssistantReviewStatus.Pending, Purpose = AssistantReviewPurpose.User, CreatedAt = f.Time.Now.AddMinutes(-1), UpdatedAt = f.Time.Now
            });
            f.Context.SaveChanges();

            (await f.Service.GetLatestAsync(UserId)).Preparing.Should().BeTrue();
        }

        [Fact]
        public async Task WhileAReviewIsMissingItStillCarriesTheLastOneThereIs()
        {
            var f = Build();
            f.AddActivityInSeptember();
            var close = await f.Service.GenerateAsync(UserId, AssistantReviewServiceTests.Close());

            // El 17 de octubre falta la mitad de mes: la tarjeta sigue mostrando el cierre de septiembre.
            var latest = await f.Service.GetLatestAsync(UserId);

            latest.State.Should().Be(AssistantLatestState.NotGenerated);
            latest.MissingKind.Should().Be(AssistantReviewKind.MidMonth);
            latest.Review.Should().NotBeNull();
            latest.Review!.Id.Should().Be(close.Id);
        }

        [Fact]
        public async Task WithNothingRunningItIsNotPreparing()
        {
            var f = Build();

            (await f.Service.GetLatestAsync(UserId)).Preparing.Should().BeFalse();
        }
    }
}
