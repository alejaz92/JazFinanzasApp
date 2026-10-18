using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 6 — costo, tope mensual, habilitación y registro de gasto, con un
    // cliente del modelo falso. Ningún test llama a la API real de Anthropic.
    public class AssistantUsageServiceTests
    {
        private const int UserId = 1;
        private const string Model = "claude-opus-5-5";
        private static readonly DateTime Now = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

        private static readonly AssistantModelRequest Request = new("instrucciones", "{}", "{\"type\":\"object\"}", AssistantEffort.High);

        private static readonly ModelPrice OpusPrice = new()
        {
            InputPerMillion = 4m,
            OutputPerMillion = 20m,
            CacheReadPerMillion = 0.20m,
            CacheWritePerMillion = 5m
        };

        private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
        }

        // Cliente falso: devuelve la respuesta cargada o lanza la excepción cargada, y cuenta las llamadas.
        private sealed class FakeModelClient : IAssistantModelClient
        {
            public int Calls { get; private set; }
            public Exception? Throw { get; set; }
            public AssistantModelResponse Response { get; set; } = Success(10_000, 4_500);

            public Task<AssistantModelResponse> GenerateAsync(AssistantModelRequest request, CancellationToken cancellationToken = default)
            {
                Calls++;
                if (Throw is not null) throw Throw;
                return Task.FromResult(Response);
            }
        }

        private static AssistantModelResponse Success(long input, long output, long cacheRead = 0, long cacheWrite = 0) =>
            new(Model, "{}", "end_turn", new AssistantModelUsage(input, output, cacheRead, cacheWrite));

        private sealed class Fixture
        {
            public ApplicationDbContext Context { get; }
            public FakeModelClient Client { get; } = new();
            public AssistantUsageService Service { get; }

            public Fixture(bool enabled = true, decimal budget = 3m, bool withPrice = true, bool withSettings = true)
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
                Context = new ApplicationDbContext(options);
                if (withSettings)
                    Context.AssistantUserSettings.Add(new AssistantUserSettings { UserId = UserId, Enabled = enabled, MonthlyBudgetUsd = budget });
                Context.SaveChanges();

                var assistantOptions = new AssistantOptions { Model = Model };
                if (withPrice) assistantOptions.Prices[Model] = OpusPrice;

                Service = new AssistantUsageService(
                    new AssistantUsageRepository(Context),
                    Client,
                    Options.Create(assistantOptions),
                    new FixedTimeProvider(Now),
                    NullLogger<AssistantUsageService>.Instance);
            }

            public void AddSpend(decimal cost, string purpose = AssistantApiCallPurpose.Review, DateTime? at = null)
            {
                Context.AssistantApiCalls.Add(new AssistantApiCall
                {
                    UserId = UserId,
                    Purpose = purpose,
                    Model = Model,
                    CostUsd = cost,
                    Succeeded = true,
                    CreatedAt = at ?? Now.AddDays(-1)
                });
                Context.SaveChanges();
            }
        }

        private static AssistantModelCall Call(string purpose = AssistantApiCallPurpose.Review, int? reviewId = null) =>
            new(UserId, purpose, "month-close.v1", reviewId, Request);

        // --- costo ---

        [Fact]
        public void TheCostWithoutCacheIsInputPlusOutputAtTheListPrice()
        {
            // 10.000 de entrada a US$ 4 y 4.500 de salida a US$ 20: 0,04 + 0,09.
            AssistantUsageService.CalculateCost(OpusPrice, new AssistantModelUsage(10_000, 4_500, 0, 0)).Should().Be(0.13m);
        }

        [Fact]
        public void TheCostWithCacheAddsReadsAndWritesAtTheirOwnPrice()
        {
            // 0,04 + 0,09 de siempre, más 8.000 leídos de caché a US$ 0,20 (0,0016) y 2.000 escritos a US$ 5 (0,01).
            AssistantUsageService.CalculateCost(OpusPrice, new AssistantModelUsage(10_000, 4_500, 8_000, 2_000)).Should().Be(0.1416m);
        }

        [Fact]
        public void ACallWithNoTokensCostsNothing()
        {
            AssistantUsageService.CalculateCost(OpusPrice, AssistantModelUsage.None).Should().Be(0m);
        }

        [Fact]
        public async Task ASuccessfulCallIsRecordedWithItsTokensAndCost()
        {
            var f = new Fixture();
            f.Client.Response = Success(10_000, 4_500, 8_000, 2_000);

            var response = await f.Service.CallModelAsync(Call(reviewId: 7));

            response.Should().BeSameAs(f.Client.Response);
            var row = f.Context.AssistantApiCalls.Single();
            row.UserId.Should().Be(UserId);
            row.Purpose.Should().Be(AssistantApiCallPurpose.Review);
            row.Model.Should().Be(Model);
            row.PromptVersion.Should().Be("month-close.v1");
            row.ReviewId.Should().Be(7);
            row.InputTokens.Should().Be(10_000);
            row.OutputTokens.Should().Be(4_500);
            row.CacheReadTokens.Should().Be(8_000);
            row.CacheWriteTokens.Should().Be(2_000);
            row.CostUsd.Should().Be(0.1416m);
            row.StopReason.Should().Be("end_turn");
            row.Succeeded.Should().BeTrue();
        }

        // --- tope ---

        [Fact]
        public async Task AtTheBudgetTheModelIsNotCalledAndNothingIsRecorded()
        {
            var f = new Fixture(budget: 3m);
            f.AddSpend(3m);

            var act = () => f.Service.CallModelAsync(Call());

            var ex = await act.Should().ThrowAsync<AssistantBudgetReachedException>();
            ex.Which.Message.Should().Contain("US$ 3,00 de US$ 3,00").And.Contain("01/11/2026");
            f.Client.Calls.Should().Be(0);
            f.Context.AssistantApiCalls.Count().Should().Be(1); // solo el gasto previo
        }

        [Fact]
        public async Task JustBelowTheBudgetTheCallRuns()
        {
            var f = new Fixture(budget: 3m);
            f.AddSpend(2.99m);

            await f.Service.CallModelAsync(Call());

            f.Client.Calls.Should().Be(1);
        }

        [Fact]
        public async Task EvalCallsDoNotCountTowardsTheBudgetButAreRecorded()
        {
            var f = new Fixture(budget: 3m);
            f.AddSpend(10m, AssistantApiCallPurpose.Eval);

            var status = await f.Service.GetStatusAsync(UserId);
            status.SpentUsd.Should().Be(0m);
            status.BudgetReached.Should().BeFalse();

            await f.Service.CallModelAsync(Call(AssistantApiCallPurpose.Eval));
            f.Context.AssistantApiCalls.Count(c => c.Purpose == AssistantApiCallPurpose.Eval).Should().Be(2);
        }

        [Fact]
        public async Task AnEvalCallRunsEvenWhenTheBudgetIsReached()
        {
            var f = new Fixture(budget: 3m);
            f.AddSpend(3m);

            await f.Service.CallModelAsync(Call(AssistantApiCallPurpose.Eval));

            f.Client.Calls.Should().Be(1);
        }

        [Fact]
        public async Task OnlyTheCurrentUtcMonthCounts()
        {
            var f = new Fixture(budget: 3m);
            f.AddSpend(9m, at: new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc));
            f.AddSpend(1m, at: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

            var status = await f.Service.GetStatusAsync(UserId);

            status.SpentUsd.Should().Be(1m);
            status.RenewsOn.Should().Be(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task PastEightyPercentTheWeeklyScanIsSkippedButTheReviewRuns()
        {
            var f = new Fixture(budget: 3m);
            f.AddSpend(2.5m); // 83 % del tope

            var scan = () => f.Service.CallModelAsync(Call(AssistantApiCallPurpose.WeeklyScan));
            await scan.Should().ThrowAsync<AssistantBudgetReachedException>();
            f.Client.Calls.Should().Be(0);

            await f.Service.CallModelAsync(Call(AssistantApiCallPurpose.Review));
            f.Client.Calls.Should().Be(1);
        }

        [Fact]
        public async Task TheWeeklyScanRunsJustUnderEightyPercent()
        {
            var f = new Fixture(budget: 3m);
            f.AddSpend(2.39m);

            await f.Service.CallModelAsync(Call(AssistantApiCallPurpose.WeeklyScan));

            f.Client.Calls.Should().Be(1);
        }

        // --- llamadas fallidas ---

        [Fact]
        public async Task AFailedCallIsStillRecordedAndTheErrorPropagates()
        {
            var f = new Fixture();
            f.Client.Throw = new AssistantModelException();

            var act = () => f.Service.CallModelAsync(Call(reviewId: 5));

            await act.Should().ThrowAsync<AssistantModelException>();
            var row = f.Context.AssistantApiCalls.Single();
            row.Succeeded.Should().BeFalse();
            row.CostUsd.Should().Be(0m);
            row.ReviewId.Should().Be(5);
            row.StopReason.Should().Be("error");
        }

        [Fact]
        public async Task ARefusalAndATruncatedResponseArePaidForButMarkedAsNotSucceeded()
        {
            var f = new Fixture();
            f.Client.Response = new AssistantModelResponse(Model, null, "refusal", new AssistantModelUsage(10_000, 100, 0, 0));
            await f.Service.CallModelAsync(Call());

            f.Client.Response = new AssistantModelResponse(Model, "{\"head", "max_tokens", new AssistantModelUsage(10_000, 16_000, 0, 0));
            await f.Service.CallModelAsync(Call());

            var rows = f.Context.AssistantApiCalls.OrderBy(c => c.Id).ToList();
            rows.Select(r => r.Succeeded).Should().AllBeEquivalentTo(false);
            rows.Select(r => r.StopReason).Should().Equal("refusal", "max_tokens");
            rows[0].CostUsd.Should().Be(0.042m);
            rows[1].CostUsd.Should().Be(0.36m);
        }

        [Fact]
        public async Task TheSpendLimitFromAnthropicIsTreatedAsTheBudgetReachedWithTheSameMessage()
        {
            // El tope propio, con el mismo gasto, da el mensaje de referencia.
            var own = new Fixture(budget: 3m);
            own.AddSpend(3m);
            var ownMessage = (await Assert.ThrowsAsync<AssistantBudgetReachedException>(() => own.Service.CallModelAsync(Call()))).Message;

            // Anthropic corta primero: la app todavía no llegó a su tope, pero el mensaje es el mismo formato.
            var f = new Fixture(budget: 3m);
            f.AddSpend(3m - 0.5m);
            f.Client.Throw = new AssistantSpendLimitException("You have reached your specified workspace API usage limits.");

            var act = () => f.Service.CallModelAsync(Call(reviewId: 9));

            var ex = await act.Should().ThrowAsync<AssistantBudgetReachedException>();
            ex.Which.Message.Should().StartWith("Se alcanzó el tope mensual de gasto del asistente");
            ownMessage.Should().StartWith("Se alcanzó el tope mensual de gasto del asistente");
            var recorded = f.Context.AssistantApiCalls.OrderBy(c => c.Id).Last();
            recorded.StopReason.Should().Be("spend_limit");
            recorded.Succeeded.Should().BeFalse();
            recorded.CostUsd.Should().Be(0m);
            recorded.ReviewId.Should().Be(9);
        }

        [Fact]
        public async Task ACancelledCallIsNotRecordedAsAFailure()
        {
            var f = new Fixture();
            f.Client.Throw = new OperationCanceledException();

            var act = () => f.Service.CallModelAsync(Call());

            await act.Should().ThrowAsync<OperationCanceledException>();
            f.Context.AssistantApiCalls.Should().BeEmpty();
        }

        // --- habilitación ---

        [Fact]
        public async Task AUserWithoutSettingsGetsTheNotEnabledError()
        {
            var f = new Fixture(withSettings: false);

            var act = () => f.Service.CallModelAsync(Call());

            (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Message.Should().Be("El asistente no está habilitado para tu usuario");
            f.Client.Calls.Should().Be(0);
            f.Context.AssistantApiCalls.Should().BeEmpty();
        }

        [Fact]
        public async Task ADisabledUserGetsTheNotEnabledErrorEvenForEval()
        {
            var f = new Fixture(enabled: false);

            var act = () => f.Service.CallModelAsync(Call(AssistantApiCallPurpose.Eval));

            await act.Should().ThrowAsync<BusinessRuleException>();
            f.Client.Calls.Should().Be(0);
            (await f.Service.GetStatusAsync(UserId)).Enabled.Should().BeFalse();
        }

        [Fact]
        public async Task EnsureEnabledPassesForAnEnabledUser()
        {
            var f = new Fixture();

            var act = () => f.Service.EnsureEnabledAsync(UserId);

            await act.Should().NotThrowAsync();
        }

        // --- configuración ---

        [Fact]
        public async Task WithoutAPriceForTheConfiguredModelItFailsBeforeCalling()
        {
            var f = new Fixture(withPrice: false);

            var act = () => f.Service.CallModelAsync(Call());

            await act.Should().ThrowAsync<InvalidOperationException>();
            f.Client.Calls.Should().Be(0);
        }

        [Fact]
        public async Task AnUnknownPurposeIsRejected()
        {
            var f = new Fixture();

            var act = () => f.Service.CallModelAsync(Call("Otra"));

            await act.Should().ThrowAsync<ArgumentException>();
            f.Client.Calls.Should().Be(0);
        }

        // --- detección del 400 de Anthropic ---

        [Theory]
        [InlineData("Your credit balance is too low to access the Anthropic API. Please go to Plans & Billing to upgrade or purchase credits.")]
        [InlineData("You have reached your specified workspace API usage limits. You will regain access on 2026-11-01 at 00:00 UTC.")]
        [InlineData("This request would exceed your workspace's monthly spend limit.")]
        [InlineData("Workspace SPENDING LIMIT reached")]
        public void TheSpendLimitMessagesAreRecognised(string message)
        {
            AssistantSpendLimitDetector.IsSpendLimitMessage(message).Should().BeTrue();
        }

        [Theory]
        [InlineData("messages: text content blocks must be non-empty")]
        [InlineData("output_config.format: schema is not supported")]
        [InlineData("")]
        [InlineData(null)]
        public void OtherBadRequestsAreNotTakenForTheSpendLimit(string? message)
        {
            AssistantSpendLimitDetector.IsSpendLimitMessage(message).Should().BeFalse();
        }

        // --- cliente real, sin red ---

        [Fact]
        public async Task TheRealClientWithoutCredentialsFailsWithAClearMessageBeforeAnyNetworkCall()
        {
            // Si esta máquina tiene credenciales de Anthropic en el entorno, el cliente las usaría y la llamada
            // saldría de verdad: en ese caso el test no corre. Ningún test llama a la API real.
            var envNames = new[] { "ANTHROPIC_API_KEY", "ANTHROPIC_AUTH_TOKEN", "ANTHROPIC_FEDERATION_RULE_ID" };
            if (envNames.Any(n => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(n)))) return;

            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            var client = new AnthropicAssistantModelClient(
                Options.Create(new AssistantOptions { Model = Model }), configuration, NullLogger<AnthropicAssistantModelClient>.Instance);

            var act = () => client.GenerateAsync(Request);

            (await act.Should().ThrowAsync<AssistantModelException>()).Which.Message.Should().Contain("falta la clave");
        }

        // --- mapeo de la respuesta del SDK ---

        private static Anthropic.Models.Beta.Messages.BetaMessage SdkMessage(string stopReason) =>
            Anthropic.Models.Beta.Messages.BetaMessage.FromRawUnchecked(
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>($$"""
                {
                  "id": "msg_test",
                  "type": "message",
                  "role": "assistant",
                  "model": "claude-opus-5-5",
                  "content": [ { "type": "text", "text": "{\"headline\":\"h\"}" } ],
                  "stop_reason": "{{stopReason}}",
                  "stop_sequence": null,
                  "usage": { "input_tokens": 11213, "output_tokens": 3320, "cache_read_input_tokens": 100, "cache_creation_input_tokens": 50 }
                }
                """)!);

        [Theory]
        [InlineData("end_turn")]
        [InlineData("refusal")]
        [InlineData("max_tokens")]
        public void TheSdkEnumsAreReadWithoutJsonQuotes(string stopReason)
        {
            // Regresión de la primera llamada real: ToString() de los enums del SDK devuelve el valor entre comillas.
            var response = AnthropicAssistantModelClient.ToResponse(SdkMessage(stopReason));

            response.Model.Should().Be("claude-opus-5-5");
            response.StopReason.Should().Be(stopReason);
            response.IsRefusal.Should().Be(stopReason == "refusal");
            response.IsTruncated.Should().Be(stopReason == "max_tokens");
            response.Usage.Should().Be(new AssistantModelUsage(11_213, 3_320, 100, 50));
            response.Text.Should().Be("{\"headline\":\"h\"}");
        }

        // --- respuesta ---

        [Fact]
        public void OnlyACompleteResponseWithTextIsUsable()
        {
            Success(1, 1).IsUsable.Should().BeTrue();
            new AssistantModelResponse(Model, null, "end_turn", AssistantModelUsage.None).IsUsable.Should().BeFalse();
            new AssistantModelResponse(Model, "x", "refusal", AssistantModelUsage.None).IsUsable.Should().BeFalse();
            new AssistantModelResponse(Model, "x", "max_tokens", AssistantModelUsage.None).IsUsable.Should().BeFalse();
        }
    }
}
