using System.Reflection;
using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.Functions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm-v2.md, Fase 8 — la Function App de la preparación de madrugada. Ningún test llama a la API real ni toca una
    // base real: la base es en memoria y el modelo es el cliente falso del v1.
    public class FunctionsTests
    {
        private static IConfiguration Configuration(Dictionary<string, string?>? extra = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["Assistant:Model"] = "claude-opus-5-5",
                ["Assistant:MaxTokens"] = "16000",
                ["Assistant:Prices:claude-opus-5-5:InputPerMillion"] = "4.00",
                ["Assistant:Prices:claude-opus-5-5:OutputPerMillion"] = "20.00",
                ["Assistant:Prices:claude-opus-5-5:CacheReadPerMillion"] = "0.20",
                ["Assistant:Prices:claude-opus-5-5:CacheWritePerMillion"] = "5.00"
            };
            if (extra != null) foreach (var pair in extra) values[pair.Key] = pair.Value;
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static ServiceCollection NewServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(configuration);
            return services;
        }

        // ── El armado de dependencias ────────────────────────────────────────────────────────────────────────────

        [Fact]
        // ValidateOnBuild: el host de Azure Functions valida el grafo entero al arrancar (lo vio el host real en la Fase 8: faltaba UserManager).
        public void TheRegistrationResolvesTheSchedulerAndEverythingItNeedsWithTheSameServicesAsTheApi()
        {
            var configuration = Configuration();
            var services = NewServices(configuration);
            services.AddFunctionsServices(configuration, o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IAssistantSchedulerService>().Should().NotBeNull();
            scope.ServiceProvider.GetRequiredService<IAssistantReviewService>().Should().NotBeNull();
            scope.ServiceProvider.GetRequiredService<IAssistantMonthReadinessService>().Should().NotBeNull();
            scope.ServiceProvider.GetRequiredService<AssistantContextBuilder>().Should().NotBeNull();
            scope.ServiceProvider.GetServices<IAssistantContextSource>().Should().HaveCount(7);
            ActivatorUtilities.CreateInstance<AssistantNightlyFunction>(scope.ServiceProvider).Should().NotBeNull();
        }

        [Fact]
        public void WithoutAConnectionStringItFailsAtStartupWithAClearMessage()
        {
            var configuration = Configuration();

            FluentActions.Invoking(() => NewServices(configuration).AddFunctionsServices(configuration))
                .Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings__JazFinanzasAppConnectionString*");
        }

        [Fact]
        public void WithAConnectionStringItRegistersSqlServerWithoutConnecting()
        {
            var configuration = Configuration(new() { ["ConnectionStrings:JazFinanzasAppConnectionString"] = "Server=(localdb)\\nunca;Database=nada;Trusted_Connection=True" });
            var services = NewServices(configuration);
            services.AddFunctionsServices(configuration);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
        }

        // ── La función ──────────────────────────────────────────────────────────────────────────────────────────

        [Fact]
        public void TheFunctionIsATimerAtFiveAndFiveThirtyArgentineTime()
        {
            var method = typeof(AssistantNightlyFunction).GetMethod(nameof(AssistantNightlyFunction.Run))!;

            method.GetCustomAttribute<FunctionAttribute>()!.Name.Should().Be("AssistantNightlyPreparation");
            var trigger = method.GetParameters()[0].GetCustomAttribute<TimerTriggerAttribute>()!;
            // 08:00 y 08:30 UTC = 05:00 y 05:30 en Argentina (sin horario de verano).
            trigger.Schedule.Should().Be("0 0,30 8 * * *");
            trigger.RunOnStartup.Should().BeFalse();
        }

        [Fact]
        public async Task TheFunctionCallsTheSchedulerOnceWithTheCancellationToken()
        {
            var scheduler = new Mock<IAssistantSchedulerService>();
            scheduler.Setup(s => s.RunAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AssistantSchedulerResult(1, AssistantSchedulerRunStatus.Completed, 2, 0, 0, null));
            var sut = new AssistantNightlyFunction(scheduler.Object, Mock.Of<ILogger<AssistantNightlyFunction>>());
            using var cts = new CancellationTokenSource();

            await sut.Run(null!, cts.Token);

            scheduler.Verify(s => s.RunAsync(cts.Token), Times.Once);
        }

        [Fact]
        public async Task AFailedRunIsLoggedAsAnError()
        {
            var scheduler = new Mock<IAssistantSchedulerService>();
            scheduler.Setup(s => s.RunAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AssistantSchedulerResult(1, AssistantSchedulerRunStatus.Failed, 0, 0, 1, "Usuario 1: falló"));
            var logger = new Mock<ILogger<AssistantNightlyFunction>>();
            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            await new AssistantNightlyFunction(scheduler.Object, logger.Object).Run(null!, CancellationToken.None);

            logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        // ── Punta a punta, con el cliente falso y una base en memoria ───────────────────────────────────────────

        [Fact]
        public async Task TheFunctionPreparesTheDueReviewsOfAnEnabledUserEndToEnd()
        {
            var dbName = Guid.NewGuid().ToString();
            var configuration = Configuration();
            var services = NewServices(configuration);
            services.AddFunctionsServices(configuration, o => o.UseInMemoryDatabase(dbName));

            // Lo único que se reemplaza: el reloj (sábado 17 de octubre de 2026), el modelo (falso), las fuentes de contexto
            // (una de prueba) y el chequeo de "mes listo" (listo). El resto es el registro real.
            var time = new AssistantReviewServiceTests.MutableTimeProvider(new DateTime(2026, 10, 17, 8, 0, 0, DateTimeKind.Utc));
            var client = new AssistantReviewServiceTests.FakeModelClient();
            services.AddSingleton<TimeProvider>(time);
            services.AddSingleton<IAssistantModelClient>(client);
            services.RemoveAll<IAssistantContextSource>();
            services.AddScoped<IAssistantContextSource, AssistantReviewServiceTests.FakeSource>();
            var readiness = new Mock<IAssistantMonthReadinessService>();
            readiness.Setup(r => r.CheckAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new AssistantMonthReadiness(true, Array.Empty<string>(), false));
            services.AddScoped(_ => readiness.Object);

            await using var provider = services.BuildServiceProvider();
            await using (var seed = provider.CreateAsyncScope())
            {
                var db = seed.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.AssistantUserSettings.Add(new AssistantUserSettings { UserId = 1, Enabled = true, MonthlyBudgetUsd = 3m });
                var account = db.Accounts.Add(new Account { Name = "Cuenta", UserId = 1 }).Entity;
                db.Transactions.Add(new Transaction { Account = account, PortfolioId = 1, AssetId = 1, Date = new DateTime(2026, 9, 10), MovementType = "E", Amount = 100m });
                db.Transactions.Add(new Transaction { Account = account, PortfolioId = 1, AssetId = 1, Date = new DateTime(2026, 10, 5), MovementType = "E", Amount = 100m });
                await db.SaveChangesAsync();
            }

            await using (var run = provider.CreateAsyncScope())
                await ActivatorUtilities.CreateInstance<AssistantNightlyFunction>(run.ServiceProvider).Run(null!, CancellationToken.None);

            await using var check = provider.CreateAsyncScope();
            var context = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.AssistantReviews.Where(r => r.Status == AssistantReviewStatus.Ready).Select(r => r.Kind)
                .Should().BeEquivalentTo(new[] { AssistantReviewKind.MonthClose, AssistantReviewKind.MidMonth });
            client.Requests.Should().HaveCount(2);
            var runRecord = context.AssistantSchedulerRuns.Single();
            runRecord.Status.Should().Be(AssistantSchedulerRunStatus.Completed);
            runRecord.Prepared.Should().Be(2);
        }
    }
}
