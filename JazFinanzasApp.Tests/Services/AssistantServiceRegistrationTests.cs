using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.Assistant.Sources;
using JazFinanzasApp.API.Infrastructure;
using JazFinanzasApp.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 4 — el grafo de dependencias del asistente se arma completo con el
    // mismo registro que usa Program.cs. Un servicio sin registrar solo se notaba al primer pedido real.
    public class AssistantServiceRegistrationTests
    {
        private static ServiceProvider BuildProvider(Dictionary<string, string?>? settings = null)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? new Dictionary<string, string?>()).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddApplicationServices(configuration);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void TheContextBuilderResolvesWithEveryRegisteredSource()
        {
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();

            var sources = scope.ServiceProvider.GetServices<IAssistantContextSource>().ToList();
            var builder = scope.ServiceProvider.GetRequiredService<AssistantContextBuilder>();

            builder.Should().NotBeNull();
            sources.Select(s => s.Key).Should().BeEquivalentTo(
                IncomeExpenseContextSource.SourceKey,
                TopExpensesContextSource.SourceKey,
                CardsContextSource.SourceKey,
                NetWorthContextSource.SourceKey,
                InvestmentsContextSource.SourceKey,
                TripsContextSource.SourceKey,
                AlertsContextSource.SourceKey);
        }

        [Fact]
        public void EverySourceDeclaresItsMeasuresAndAPrimaryCurrency()
        {
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();

            foreach (var source in scope.ServiceProvider.GetServices<IAssistantContextSource>())
            {
                source.Measures.Should().NotBeNullOrWhiteSpace(source.Key);
                source.PrimaryCurrency.Should().BeOneOf("ARS", "USD");
            }
        }

        [Fact]
        public void TheAssistantOptionsAreBoundFromTheConfiguration()
        {
            using var provider = BuildProvider(new Dictionary<string, string?>
            {
                ["Assistant:Model"] = "claude-opus-5-5",
                ["Assistant:MaxTokens"] = "12000",
                ["Assistant:Prices:claude-opus-5-5:InputPerMillion"] = "4.00",
                ["Assistant:Prices:claude-opus-5-5:OutputPerMillion"] = "20.00",
                ["Assistant:Prices:claude-opus-5-5:CacheReadPerMillion"] = "0.20",
                ["Assistant:Prices:claude-opus-5-5:CacheWritePerMillion"] = "5.00"
            });

            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AssistantOptions>>().Value;

            options.Model.Should().Be("claude-opus-5-5");
            options.MaxTokens.Should().Be(12000);
            options.GetPrice("claude-opus-5-5")!.OutputPerMillion.Should().Be(20.00m);
            options.GetPrice("otro-modelo").Should().BeNull();
        }
    }
}
