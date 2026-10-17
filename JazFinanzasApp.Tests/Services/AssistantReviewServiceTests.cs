using System.Text.Json;
using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.DTO.Assistant;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 7 — generación, estados, regeneración, control de cifras, calificación y
    // comparación de devoluciones, con un cliente del modelo falso. Ningún test llama a la API real.
    public class AssistantReviewServiceTests
    {
        internal const int UserId = 1;
        private const string Model = "claude-opus-5-5";

        // El contexto que arma la fuente falsa: estos son los números que el control de cifras conoce.
        private static readonly object FakeData = new { total = 85432, pct = 17.8 };

        internal static readonly string GoodOutput = JsonSerializer.Serialize(new
        {
            headline = "El mes cerró con un gasto de $85.000",
            highlights = new[] { "Primero", "Segundo", "Tercero" },
            body = "El ahorro fue de 18 %."
        });

        internal sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
        {
            public DateTime Now { get; set; } = utcNow;
            public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
            public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        }

        internal sealed class FakeSource : IAssistantContextSource
        {
            public string Key => "fake";
            public string Measures => "una fuente de prueba";
            public string PrimaryCurrency => "ARS";
            public Task<object?> BuildAsync(AssistantContextRequest request) => Task.FromResult<object?>(FakeData);
        }

        internal sealed class FakeModelClient : IAssistantModelClient
        {
            public List<AssistantModelRequest> Requests { get; } = new();
            public Func<AssistantModelResponse> Respond { get; set; } = () => Ok(GoodOutput);
            public Exception? Throw { get; set; }
            public Task? Gate { get; set; }
            public TaskCompletionSource? Entered { get; set; }

            public async Task<AssistantModelResponse> GenerateAsync(AssistantModelRequest request, CancellationToken cancellationToken = default)
            {
                Requests.Add(request);
                Entered?.TrySetResult();
                if (Gate != null) await Gate;
                if (Throw != null) throw Throw;
                return Respond();
            }
        }

        internal static AssistantModelResponse Ok(string text) =>
            new(Model, text, "end_turn", new AssistantModelUsage(9_000, 4_000, 0, 0));

        internal sealed class Fixture
        {
            public ApplicationDbContext Context { get; }
            public FakeModelClient Client { get; }
            public MutableTimeProvider Time { get; }
            public AssistantReviewService Service { get; }
            public AssistantFindingService Findings { get; }
            public AssistantUsageService Usage { get; }

            public Fixture(string? dbName = null, FakeModelClient? client = null, MutableTimeProvider? time = null,
                bool enabled = true, decimal budget = 3m)
            {
                dbName ??= Guid.NewGuid().ToString();
                Client = client ?? new FakeModelClient();
                Time = time ?? new MutableTimeProvider(new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc));

                var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options;
                Context = new ApplicationDbContext(options);
                if (!Context.AssistantUserSettings.Any(s => s.UserId == UserId))
                {
                    Context.AssistantUserSettings.Add(new AssistantUserSettings { UserId = UserId, Enabled = enabled, MonthlyBudgetUsd = budget });
                    Context.SaveChanges();
                }

                var assistantOptions = new AssistantOptions { Model = Model };
                assistantOptions.Prices[Model] = new ModelPrice { InputPerMillion = 4m, OutputPerMillion = 20m, CacheReadPerMillion = 0.2m, CacheWritePerMillion = 5m };

                var usage = new AssistantUsageService(
                    new AssistantUsageRepository(Context), Client, Options.Create(assistantOptions), Time,
                    NullLogger<AssistantUsageService>.Instance);

                Usage = usage;
                Findings = new AssistantFindingService(
                    new AssistantFindingRepository(Context), new TransactionClassRepository(Context), usage, Time);

                Service = new AssistantReviewService(
                    new AssistantReviewRepository(Context), usage,
                    new AssistantContextBuilder(new IAssistantContextSource[] { new FakeSource() }),
                    Findings, Time, NullLogger<AssistantReviewService>.Instance);
            }

            // Un movimiento del usuario en una fecha, para que el período no cuente como "sin movimientos".
            public void AddActivity(DateTime date, DateTime? loadedAt = null)
            {
                var account = Context.Accounts.FirstOrDefault(a => a.UserId == UserId)
                              ?? Context.Accounts.Add(new Account { Name = "Cuenta", UserId = UserId }).Entity;
                Context.Transactions.Add(new Transaction { Account = account, AccountId = account.Id, PortfolioId = 1, AssetId = 1, Date = date, MovementType = "E", Amount = 100m, CreatedAt = loadedAt ?? Time.Now });
                Context.SaveChanges();
            }

            public void AddActivityInSeptember() => AddActivity(new DateTime(2026, 9, 10));
            public void AddActivityInOctober() => AddActivity(new DateTime(2026, 10, 5));
        }

        internal static GenerateAssistantReviewDTO Close(bool regenerate = false) =>
            new() { Kind = AssistantReviewKind.MonthClose, Regenerate = regenerate };

        internal static GenerateAssistantReviewDTO Mid(bool regenerate = false) =>
            new() { Kind = AssistantReviewKind.MidMonth, Regenerate = regenerate };

        // --- reviews/latest (D6) ---

        [Fact]
        public async Task WithoutTheCloseOfThePreviousMonthItAsksForTheClose()
        {
            var f = new Fixture();

            var latest = await f.Service.GetLatestAsync(UserId);

            latest.State.Should().Be(AssistantLatestState.NotGenerated);
            latest.MissingKind.Should().Be(AssistantReviewKind.MonthClose);
            latest.MissingPeriodStart.Should().Be(new DateTime(2026, 9, 1));
        }

        [Fact]
        public async Task TheCloseComesBeforeTheMidMonthEvenFromDay15()
        {
            var f = new Fixture(); // 17 de octubre: faltan las dos

            (await f.Service.GetLatestAsync(UserId)).MissingKind.Should().Be(AssistantReviewKind.MonthClose);

            f.AddActivityInSeptember();
            await f.Service.GenerateAsync(UserId, Close());

            var next = await f.Service.GetLatestAsync(UserId);
            next.State.Should().Be(AssistantLatestState.NotGenerated);
            next.MissingKind.Should().Be(AssistantReviewKind.MidMonth);
            next.MissingPeriodStart.Should().Be(new DateTime(2026, 10, 1));
        }

        [Fact]
        public async Task TheMidMonthIsNotRequiredBeforeDay15()
        {
            var f = new Fixture(time: new MutableTimeProvider(new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc)));
            f.AddActivityInSeptember();
            await f.Service.GenerateAsync(UserId, Close());

            var latest = await f.Service.GetLatestAsync(UserId);

            latest.State.Should().Be(AssistantLatestState.Ready);
            latest.Review!.Kind.Should().Be(AssistantReviewKind.MonthClose);
        }

        [Fact]
        public async Task OnDay15TheMidMonthIsRequiredAndAfterGeneratingItTheLatestIsThatOne()
        {
            var f = new Fixture(time: new MutableTimeProvider(new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc)));
            f.AddActivityInSeptember();
            f.AddActivityInOctober();
            await f.Service.GenerateAsync(UserId, Close());
            (await f.Service.GetLatestAsync(UserId)).MissingKind.Should().Be(AssistantReviewKind.MidMonth);

            await f.Service.GenerateAsync(UserId, Mid());

            var latest = await f.Service.GetLatestAsync(UserId);
            latest.State.Should().Be(AssistantLatestState.Ready);
            latest.Review!.Kind.Should().Be(AssistantReviewKind.MidMonth);
            latest.Review.CutoffDate.Should().Be(new DateTime(2026, 10, 14));
        }

        [Fact]
        public async Task WhenNothingIsMissingButThereIsNoTextTheLatestIsEmpty()
        {
            var f = new Fixture(time: new MutableTimeProvider(new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)));
            await f.Service.GenerateAsync(UserId, Close()); // sin movimientos en septiembre

            var latest = await f.Service.GetLatestAsync(UserId);

            latest.State.Should().Be(AssistantLatestState.Empty);
            latest.Review.Should().BeNull();
        }

        // --- generar ---

        [Fact]
        public async Task GeneratingSavesTheReviewWithItsContextVersionModelAndCost()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Ready);
            dto.IsCurrent.Should().BeTrue();
            dto.Kind.Should().Be(AssistantReviewKind.MonthClose);
            dto.PeriodStart.Should().Be(new DateTime(2026, 9, 1));
            dto.CutoffDate.Should().Be(new DateTime(2026, 9, 30));
            dto.Headline.Should().Be("El mes cerró con un gasto de $85.000");
            dto.Highlights.Should().Equal("Primero", "Segundo", "Tercero");
            dto.Body.Should().Be("El ahorro fue de 18 %.");
            dto.PromptVersion.Should().Be("month-close.v1");
            dto.Model.Should().Be(Model);
            dto.UnverifiedFigures.Should().BeEmpty();

            var row = f.Context.AssistantReviews.Single();
            row.ContextJson.Should().Contain("\"fake\"").And.Contain("85432");
            var call = f.Context.AssistantApiCalls.Single();
            call.ReviewId.Should().Be(row.Id);
            call.Purpose.Should().Be(AssistantApiCallPurpose.Review);
            call.PromptVersion.Should().Be("month-close.v1");
            call.CostUsd.Should().Be(0.116m);

            f.Client.Requests.Should().ContainSingle();
            f.Client.Requests[0].Effort.Should().Be(AssistantEffort.High);
            f.Client.Requests[0].UserContent.Should().Be(row.ContextJson);
            f.Client.Requests[0].SystemPrompt.Should().Contain("CIERRE DE MES");
        }

        [Fact]
        public async Task AMidMonthUsesItsOwnInstructionsAndCutsAtYesterday()
        {
            var f = new Fixture();
            f.AddActivityInOctober();

            var dto = await f.Service.GenerateAsync(UserId, Mid());

            dto.PromptVersion.Should().Be("mid-month.v1");
            dto.PeriodStart.Should().Be(new DateTime(2026, 10, 1));
            dto.CutoffDate.Should().Be(new DateTime(2026, 10, 16));
            f.Client.Requests[0].SystemPrompt.Should().Contain("MITAD DE MES");
        }

        [Fact]
        public async Task GeneratingAgainWithoutRegeneratingReturnsTheExistingOneAndDoesNotCallTheModel()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var first = await f.Service.GenerateAsync(UserId, Close());

            var second = await f.Service.GenerateAsync(UserId, Close());

            second.Id.Should().Be(first.Id);
            f.Client.Requests.Should().ContainSingle();
            f.Context.AssistantReviews.Count().Should().Be(1);
        }

        [Fact]
        public async Task RegeneratingKeepsThePreviousVersionAndMakesTheNewOneCurrent()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var first = await f.Service.GenerateAsync(UserId, Close());

            var second = await f.Service.GenerateAsync(UserId, Close(regenerate: true));

            second.Id.Should().NotBe(first.Id);
            var rows = f.Context.AssistantReviews.OrderBy(r => r.Id).ToList();
            rows.Should().HaveCount(2);
            rows[0].IsCurrent.Should().BeFalse();
            rows[0].Status.Should().Be(AssistantReviewStatus.Ready);
            rows[1].IsCurrent.Should().BeTrue();
            (await f.Service.GetHistoryAsync(UserId, null)).Should().ContainSingle().Which.Id.Should().Be(second.Id);
        }

        [Fact]
        public async Task APeriodWithoutMovementsDoesNotCallTheModelAndIsNotRetried()
        {
            var f = new Fixture();

            var first = await f.Service.GenerateAsync(UserId, Close());
            var second = await f.Service.GenerateAsync(UserId, Close());

            first.Status.Should().Be(AssistantReviewStatus.NoActivity);
            second.Id.Should().Be(first.Id);
            f.Client.Requests.Should().BeEmpty();
            f.Context.AssistantApiCalls.Should().BeEmpty();
            (await f.Service.GetLatestAsync(UserId)).MissingKind.Should().NotBe(AssistantReviewKind.MonthClose);
        }

        [Fact]
        public async Task AChargedCardPurchaseCountsAsActivity()
        {
            var f = new Fixture();
            f.Context.CardTransactions.Add(new CardTransaction { UserId = UserId, Date = new DateTime(2026, 9, 20), Detail = "Compra", Repeat = "N" });
            f.Context.SaveChanges();

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Ready);
        }

        // --- estados y fallas ---

        [Fact]
        public async Task ARefusalLeavesTheReviewFailedWithAMessage()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => new AssistantModelResponse(Model, null, "refusal", new AssistantModelUsage(9_000, 50, 0, 0));

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Failed);
            dto.ErrorMessage.Should().Contain("no pudo responder");
            dto.IsCurrent.Should().BeTrue("no hay otra buena del período a la que taparle el lugar");
            f.Context.AssistantApiCalls.Single().Succeeded.Should().BeFalse();
            f.Context.AssistantApiCalls.Single().CostUsd.Should().BeGreaterThan(0m, "una negativa se paga igual");
        }

        [Fact]
        public async Task ATruncatedResponseLeavesTheReviewFailedWithAMessage()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => new AssistantModelResponse(Model, "{\"headline\":\"Cor", "max_tokens", new AssistantModelUsage(9_000, 16_000, 0, 0));

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Failed);
            dto.ErrorMessage.Should().Contain("se cortó");
        }

        [Theory]
        [InlineData("esto no es json")]
        [InlineData("{\"headline\":\"\",\"highlights\":[\"a\"],\"body\":\"x\"}")]
        [InlineData("{\"headline\":\"h\",\"highlights\":[],\"body\":\"x\"}")]
        [InlineData("{\"headline\":\"h\",\"highlights\":[\"a\"],\"body\":\"  \"}")]
        public async Task AnIncompleteOrMalformedResponseLeavesTheReviewFailed(string text)
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(text);

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Failed);
            dto.ErrorMessage.Should().Contain("formato esperado");
        }

        [Fact]
        public async Task OnlyTheFirstThreeHighlightsAreKept()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(JsonSerializer.Serialize(new { headline = "h", highlights = new[] { "a", "b", "c", "d" }, body = "x" }));

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Highlights.Should().Equal("a", "b", "c");
        }

        [Fact]
        public async Task AModelFailureLeavesTheReviewFailedWithoutThrowing()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Throw = new AssistantModelException();

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Failed);
            dto.ErrorMessage.Should().Be(AssistantModelException.DefaultMessage);
        }

        [Fact]
        public async Task AnUnexpectedErrorLeavesTheReviewFailedWithAGenericMessage()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Throw = new InvalidOperationException("detalle técnico que el usuario no tiene por qué ver");

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Failed);
            dto.ErrorMessage.Should().NotContain("detalle técnico");
        }

        [Fact]
        public async Task TheBudgetReachedLeavesTheReviewFailedWithTheMessageAndWithoutCallingTheModel()
        {
            var f = new Fixture(budget: 3m);
            f.AddActivityInSeptember();
            f.Context.AssistantApiCalls.Add(new AssistantApiCall { UserId = UserId, Purpose = AssistantApiCallPurpose.Review, Model = Model, CostUsd = 3m, Succeeded = true, CreatedAt = new DateTime(2026, 10, 2) });
            f.Context.SaveChanges();

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Failed);
            dto.ErrorMessage.Should().Contain("tope mensual").And.Contain("01/11/2026");
            f.Client.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task ACancellationLeavesTheReviewFailedAndPropagates()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Throw = new OperationCanceledException();

            var act = () => f.Service.GenerateAsync(UserId, Close());

            await act.Should().ThrowAsync<OperationCanceledException>();
            f.Context.AssistantReviews.Single().Status.Should().Be(AssistantReviewStatus.Failed);
        }

        [Fact]
        public async Task AFailedRegenerationDoesNotReplaceTheGoodCurrentOne()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var good = await f.Service.GenerateAsync(UserId, Close());
            f.Client.Throw = new AssistantModelException();

            var failed = await f.Service.GenerateAsync(UserId, Close(regenerate: true));

            failed.Status.Should().Be(AssistantReviewStatus.Failed);
            failed.IsCurrent.Should().BeFalse();
            f.Context.AssistantReviews.Single(r => r.Id == good.Id).IsCurrent.Should().BeTrue();
            (await f.Service.GetHistoryAsync(UserId, null)).Should().ContainSingle().Which.Id.Should().Be(good.Id);
        }

        [Fact]
        public async Task AFailedReviewIsNotRetriedOnItsOwnUntilTheCooldownPasses()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Throw = new AssistantModelException();
            var failed = await f.Service.GenerateAsync(UserId, Close());

            // Dentro de la hora: la devolución fallida cubre el período y no se vuelve a llamar.
            f.Client.Throw = null;
            f.Time.Now = f.Time.Now.AddMinutes(30);
            (await f.Service.GetLatestAsync(UserId)).MissingKind.Should().NotBe(AssistantReviewKind.MonthClose);
            (await f.Service.GenerateAsync(UserId, Close())).Id.Should().Be(failed.Id);
            f.Client.Requests.Should().ContainSingle();

            // Pasada la hora, se reintenta y la nueva reemplaza a la fallida.
            f.Time.Now = f.Time.Now.AddHours(2);
            (await f.Service.GetLatestAsync(UserId)).MissingKind.Should().Be(AssistantReviewKind.MonthClose);
            var retried = await f.Service.GenerateAsync(UserId, Close());
            retried.Status.Should().Be(AssistantReviewStatus.Ready);
            f.Context.AssistantReviews.Single(r => r.Id == failed.Id).IsCurrent.Should().BeFalse();
        }

        [Fact]
        public async Task ARegenerationAfterAFailureIsAllowedRightAway()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Throw = new AssistantModelException();
            await f.Service.GenerateAsync(UserId, Close());
            f.Client.Throw = null;

            var retried = await f.Service.GenerateAsync(UserId, Close(regenerate: true));

            retried.Status.Should().Be(AssistantReviewStatus.Ready);
        }

        // --- simultaneidad ---

        [Fact]
        public async Task TwoSimultaneousGenerationsOfTheSamePeriodDoNotCreateTwoReviews()
        {
            var dbName = Guid.NewGuid().ToString();
            var gate = new TaskCompletionSource();
            var entered = new TaskCompletionSource();
            var slowClient = new FakeModelClient { Gate = gate.Task, Entered = entered };
            var first = new Fixture(dbName, slowClient);
            first.AddActivityInSeptember();
            var second = new Fixture(dbName, new FakeModelClient());

            var firstRun = first.Service.GenerateAsync(UserId, Close());
            await entered.Task;     // la primera ya tiene su fila Pending y está esperando al modelo

            var act = () => second.Service.GenerateAsync(UserId, Close());
            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Ya se está generando*");
            second.Client.Requests.Should().BeEmpty();

            gate.SetResult();
            (await firstRun).Status.Should().Be(AssistantReviewStatus.Ready);
            second.Context.AssistantReviews.Count().Should().Be(1);
        }

        [Fact]
        public async Task AStalePendingReviewIsMarkedAsInterruptedAndThePeriodCanBeGeneratedAgain()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Context.AssistantReviews.Add(new AssistantReview
            {
                UserId = UserId, Kind = AssistantReviewKind.MonthClose, PeriodStart = new DateTime(2026, 9, 1), CutoffDate = new DateTime(2026, 9, 30),
                Status = AssistantReviewStatus.Pending, Purpose = AssistantReviewPurpose.User, CreatedAt = f.Time.Now.AddMinutes(-10)
            });
            f.Context.SaveChanges();

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Ready);
            var stale = f.Context.AssistantReviews.OrderBy(r => r.Id).First();
            stale.Status.Should().Be(AssistantReviewStatus.Failed);
            stale.ErrorMessage.Should().Contain("interrumpió");
        }

        // --- control de cifras ---

        [Fact]
        public async Task AFigureThatIsNotInTheContextIsSavedAsUnverified()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(JsonSerializer.Serialize(new { headline = "Gastó $999.999", highlights = new[] { "a" }, body = "El ahorro fue de 18 %." }));

            var dto = await f.Service.GenerateAsync(UserId, Close());

            dto.Status.Should().Be(AssistantReviewStatus.Ready, "el control no bloquea");
            dto.UnverifiedFigures.Should().Equal("$999.999");
            (await f.Service.GetHistoryAsync(UserId, null)).Single().HasUnverifiedFigures.Should().BeTrue();
        }

        // --- períodos inválidos ---

        [Theory]
        [InlineData(AssistantReviewKind.MonthClose, 2026, 10)]   // el mes en curso no terminó
        [InlineData(AssistantReviewKind.MonthClose, 2026, 11)]
        [InlineData(AssistantReviewKind.MidMonth, 2026, 9)]      // la mitad de mes es solo del mes en curso
        [InlineData(AssistantReviewKind.WeeklyScan, 2026, 10)]   // todavía no disponible
        [InlineData("Otra", 2026, 9)]
        public async Task AnInvalidKindOrPeriodIsRejected(string kind, int year, int month)
        {
            var f = new Fixture();

            var act = () => f.Service.GenerateAsync(UserId, new GenerateAssistantReviewDTO { Kind = kind, PeriodStart = new DateTime(year, month, 1) });

            await act.Should().ThrowAsync<BusinessRuleException>();
            f.Client.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task AnOlderCloseCanBeGeneratedExplicitly()
        {
            var f = new Fixture();
            f.AddActivity(new DateTime(2026, 4, 12));

            var dto = await f.Service.GenerateAsync(UserId, new GenerateAssistantReviewDTO { Kind = AssistantReviewKind.MonthClose, PeriodStart = new DateTime(2026, 4, 15) });

            dto.PeriodStart.Should().Be(new DateTime(2026, 4, 1));
            dto.CutoffDate.Should().Be(new DateTime(2026, 4, 30));
        }

        [Fact]
        public async Task TheMidMonthOnTheFirstDayHasNoDataYet()
        {
            var f = new Fixture(time: new MutableTimeProvider(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)));

            var act = () => f.Service.GenerateAsync(UserId, Mid());

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // --- habilitación ---

        [Fact]
        public async Task EveryEndpointButStatusRejectsAUserWithoutTheAssistant()
        {
            var f = new Fixture(enabled: false);

            (await f.Service.GetStatusAsync(UserId)).Enabled.Should().BeFalse();
            foreach (var call in new Func<Task>[]
            {
                () => f.Service.GetLatestAsync(UserId),
                () => f.Service.GetHistoryAsync(UserId, null),
                () => f.Service.GetByIdAsync(UserId, 1),
                () => f.Service.GenerateAsync(UserId, Close()),
                () => f.Service.PreviewAsync(UserId, 1, "v1"),
                () => f.Service.SetFeedbackAsync(UserId, 1, new SetAssistantFeedbackDTO { Rating = 1 })
            })
            {
                await call.Should().ThrowAsync<BusinessRuleException>().WithMessage("El asistente no está habilitado para tu usuario");
            }
            f.Client.Requests.Should().BeEmpty();
        }

        // --- lectura ---

        [Fact]
        public async Task AReviewOfAnotherUserIsNotFound()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var dto = await f.Service.GenerateAsync(UserId, Close());
            f.Context.AssistantUserSettings.Add(new AssistantUserSettings { UserId = 2, Enabled = true, MonthlyBudgetUsd = 3m });
            f.Context.SaveChanges();

            var act = () => f.Service.GetByIdAsync(2, dto.Id);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task TheHistoryFiltersByKindAndLeavesOutEvalAndNoActivityReviews()
        {
            var f = new Fixture(time: new MutableTimeProvider(new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc)));
            f.AddActivityInSeptember();
            f.AddActivityInOctober();
            var close = await f.Service.GenerateAsync(UserId, Close());
            var mid = await f.Service.GenerateAsync(UserId, Mid());
            await f.Service.GenerateAsync(UserId, new GenerateAssistantReviewDTO { Kind = AssistantReviewKind.MonthClose, PeriodStart = new DateTime(2026, 7, 1) }); // sin movimientos
            await f.Service.PreviewAsync(UserId, close.Id, "v1");

            var all = await f.Service.GetHistoryAsync(UserId, null);
            all.Select(r => r.Id).Should().Equal(mid.Id, close.Id);
            (await f.Service.GetHistoryAsync(UserId, AssistantReviewKind.MonthClose)).Should().ContainSingle().Which.Id.Should().Be(close.Id);
            (await f.Service.GetHistoryAsync(UserId, AssistantReviewKind.MidMonth)).Should().ContainSingle().Which.Id.Should().Be(mid.Id);
            await FluentActions.Awaiting(() => f.Service.GetHistoryAsync(UserId, AssistantReviewKind.WeeklyScan)).Should().ThrowAsync<BusinessRuleException>();
        }

        // --- calificación ---

        [Fact]
        public async Task RatingAgainReplacesThePreviousRating()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var review = await f.Service.GenerateAsync(UserId, Close());

            await f.Service.SetFeedbackAsync(UserId, review.Id, new SetAssistantFeedbackDTO { Rating = 1, Comment = "  muy buena  " });
            var second = await f.Service.SetFeedbackAsync(UserId, review.Id, new SetAssistantFeedbackDTO { Rating = -1, HasWrongFigures = true, Comment = "esto ya lo sabía" });

            second.Rating.Should().Be(-1);
            second.HasWrongFigures.Should().BeTrue();
            var row = f.Context.AssistantFeedbacks.Single();
            row.Rating.Should().Be(-1);
            row.Comment.Should().Be("esto ya lo sabía");
            (await f.Service.GetByIdAsync(UserId, review.Id)).Feedback!.Rating.Should().Be(-1);
            (await f.Service.GetHistoryAsync(UserId, null)).Single().Rating.Should().Be(-1);
        }

        [Fact]
        public async Task TheCommentIsTrimmedAndAnEmptyOneIsStoredAsNull()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var review = await f.Service.GenerateAsync(UserId, Close());

            await f.Service.SetFeedbackAsync(UserId, review.Id, new SetAssistantFeedbackDTO { Rating = 1, Comment = "   " });

            f.Context.AssistantFeedbacks.Single().Comment.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public async Task ARatingThatIsNotUsefulOrNotUsefulIsRejected(int rating)
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var review = await f.Service.GenerateAsync(UserId, Close());

            var act = () => f.Service.SetFeedbackAsync(UserId, review.Id, new SetAssistantFeedbackDTO { Rating = rating });

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ATooLongCommentIsRejected()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var review = await f.Service.GenerateAsync(UserId, Close());

            var act = () => f.Service.SetFeedbackAsync(UserId, review.Id, new SetAssistantFeedbackDTO { Rating = 1, Comment = new string('x', 1001) });

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task OnlyAFinishedReviewCanBeRated()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Throw = new AssistantModelException();
            var failed = await f.Service.GenerateAsync(UserId, Close());

            var act = () => f.Service.SetFeedbackAsync(UserId, failed.Id, new SetAssistantFeedbackDTO { Rating = 1 });

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // --- preview (D10) ---

        [Fact]
        public async Task APreviewRunsOverTheSameContextAsAnEvalWithoutReplacingTheCurrentReview()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var original = await f.Service.GenerateAsync(UserId, Close());
            var originalContext = f.Context.AssistantReviews.Single().ContextJson;

            var preview = await f.Service.PreviewAsync(UserId, original.Id, "v1");

            preview.Id.Should().NotBe(original.Id);
            preview.Purpose.Should().Be(AssistantReviewPurpose.Eval);
            preview.IsCurrent.Should().BeFalse();
            preview.Status.Should().Be(AssistantReviewStatus.Ready);
            preview.PromptVersion.Should().Be("month-close.v1");
            f.Client.Requests.Last().UserContent.Should().Be(originalContext);
            f.Context.AssistantReviews.Single(r => r.Id == original.Id).IsCurrent.Should().BeTrue();
            (await f.Service.GetLatestAsync(UserId)).MissingKind.Should().NotBe(AssistantReviewKind.MonthClose);
        }

        [Fact]
        public async Task APreviewIsRegisteredAsEvalAndDoesNotCountForTheBudget()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var original = await f.Service.GenerateAsync(UserId, Close());
            var spentBefore = (await f.Service.GetStatusAsync(UserId)).SpentUsd;

            var preview = await f.Service.PreviewAsync(UserId, original.Id, "month-close.v1");

            var call = f.Context.AssistantApiCalls.Single(c => c.ReviewId == preview.Id);
            call.Purpose.Should().Be(AssistantApiCallPurpose.Eval);
            call.CostUsd.Should().BeGreaterThan(0m);
            (await f.Service.GetStatusAsync(UserId)).SpentUsd.Should().Be(spentBefore);
        }

        [Fact]
        public async Task APreviewWithAnUnknownVersionIsRejected()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            var original = await f.Service.GenerateAsync(UserId, Close());

            var act = () => f.Service.PreviewAsync(UserId, original.Id, "v9");

            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*No existe la versión*");
            f.Client.Requests.Should().ContainSingle();
        }

        [Fact]
        public async Task APreviewOfAReviewWithoutSavedContextIsRejected()
        {
            var f = new Fixture();
            var noActivity = await f.Service.GenerateAsync(UserId, Close());

            var act = () => f.Service.PreviewAsync(UserId, noActivity.Id, "v1");

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // --- instrucciones ---

        [Theory]
        [InlineData(AssistantReviewKind.MonthClose, "month-close.v1")]
        [InlineData(AssistantReviewKind.MidMonth, "mid-month.v1")]
        public void EveryCurrentPromptVersionExistsAndHasContent(string kind, string version)
        {
            AssistantPrompts.CurrentVersion(kind).Should().Be(version);
            AssistantPrompts.Load(version).Should().Contain("# Qué devolvés").And.Contain("Nunca calcules");
        }

        [Fact]
        public void AVersionIsResolvedFromItsShortNameButNotAcrossKinds()
        {
            AssistantPrompts.ResolveVersion(AssistantReviewKind.MonthClose, "v1").Should().Be("month-close.v1");
            AssistantPrompts.ResolveVersion(AssistantReviewKind.MidMonth, "mid-month.v1").Should().Be("mid-month.v1");
            FluentActions.Invoking(() => AssistantPrompts.ResolveVersion(AssistantReviewKind.MonthClose, "mid-month.v1")).Should().Throw<BusinessRuleException>();
            FluentActions.Invoking(() => AssistantPrompts.ResolveVersion(AssistantReviewKind.MonthClose, "../v1")).Should().Throw<BusinessRuleException>();
            FluentActions.Invoking(() => AssistantPrompts.ResolveVersion(AssistantReviewKind.MonthClose, "")).Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void TheOutputSchemaIsValidJsonWithTheThreeFields()
        {
            using var doc = JsonDocument.Parse(AssistantPrompts.ReviewOutputSchema);
            doc.RootElement.GetProperty("required").EnumerateArray().Select(e => e.GetString())
                .Should().BeEquivalentTo("headline", "highlights", "body", "findings");
        }
    }
}
