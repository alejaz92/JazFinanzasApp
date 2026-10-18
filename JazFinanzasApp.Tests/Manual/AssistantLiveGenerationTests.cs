using System.Text;
using System.Text.Json;
using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure;
using JazFinanzasApp.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JazFinanzasApp.Tests.Manual
{
    // plan-devoluciones-llm.md, Fase 7 — UNA llamada real al modelo con el contexto real de un usuario, para ver
    // cómo salen las instrucciones y confirmar que la API acepta el pedido (respaldo, beta, esquema). Cuesta unos
    // centavos y manda datos financieros reales a Anthropic, así que solo corre si se lo pide.
    //
    // Es SOLO LECTURA sobre la base: arma el contexto con las fuentes (que solo consultan) y llama al cliente del
    // modelo directamente, sin pasar por el servicio de uso ni el de devoluciones, así que no escribe ninguna fila.
    // No levanta la aplicación (su arranque correría las migraciones pendientes sobre la base configurada).
    //
    // La clave se lee de un archivo y nunca se imprime ni se escribe en ningún resultado.
    //
    // Variables de entorno:
    //   RUN_ASSISTANT_LIVE=1                   lo habilita
    //   ASSISTANT_LIVE_KEY_FILE=<archivo>      archivo con la clave, fuera de los repos y de OneDrive
    //   ASSISTANT_LIVE_OUT=<carpeta>           dónde escribir el resultado (nunca dentro del repo)
    //   ASSISTANT_LIVE_USER_ID=<id>            usuario (por defecto 2, ajazmatie)
    //   ASSISTANT_LIVE_KIND=MonthClose|MidMonth|WeeklyScan  tipo de pasada (por defecto MonthClose)
    //   ASSISTANT_LIVE_MONTH=yyyy-MM           mes del cierre (por defecto el mes anterior)
    public class AssistantLiveGenerationTests
    {
        [ManualFact("RUN_ASSISTANT_LIVE")]
        public async Task GenerateOneRealReviewWithTheRealModel()
        {
            var keyFile = Environment.GetEnvironmentVariable("ASSISTANT_LIVE_KEY_FILE");
            keyFile.Should().NotBeNullOrWhiteSpace("hay que indicar el archivo de la clave (ASSISTANT_LIVE_KEY_FILE)");
            File.Exists(keyFile).Should().BeTrue("el archivo de la clave tiene que existir");
            var apiKey = File.ReadAllText(keyFile!).Trim();
            apiKey.Should().NotBeNullOrWhiteSpace();

            var outDir = Environment.GetEnvironmentVariable("ASSISTANT_LIVE_OUT");
            outDir.Should().NotBeNullOrWhiteSpace("hay que indicar dónde escribir (ASSISTANT_LIVE_OUT)");
            Directory.CreateDirectory(outDir!);

            var userId = int.Parse(Environment.GetEnvironmentVariable("ASSISTANT_LIVE_USER_ID") ?? "2");
            var kind = Environment.GetEnvironmentVariable("ASSISTANT_LIVE_KIND") ?? AssistantReviewKind.MonthClose;
            kind.Should().BeOneOf(AssistantReviewKind.MonthClose, AssistantReviewKind.MidMonth, AssistantReviewKind.WeeklyScan);

            var apiDirectory = FindApiDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiDirectory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddInMemoryCollection(new Dictionary<string, string?> { [AnthropicAssistantModelClient.ApiKeySetting] = apiKey })
                .Build();
            var connectionString = configuration.GetConnectionString("JazFinanzasAppConnectionString");
            connectionString.Should().NotBeNullOrWhiteSpace();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration); // el host real ya lo registra
            services.AddDbContext<ApplicationDbContext>(o => o
                .UseSqlServer(connectionString, s => s.CommandTimeout(120))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
            services.AddApplicationServices(configuration);

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var builder = scope.ServiceProvider.GetRequiredService<AssistantContextBuilder>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<AssistantOptions>>().Value;

            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            AssistantContextRequest request;
            if (kind == AssistantReviewKind.MidMonth)
            {
                request = new AssistantContextRequest(userId, kind, thisMonth, today.AddDays(-1));
            }
            else if (kind == AssistantReviewKind.WeeklyScan)
            {
                var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                request = new AssistantContextRequest(userId, kind, monday, today.AddDays(-1));
            }
            else
            {
                var monthText = Environment.GetEnvironmentVariable("ASSISTANT_LIVE_MONTH");
                var month = monthText == null
                    ? thisMonth.AddMonths(-1)
                    : DateTime.ParseExact(monthText, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
                request = new AssistantContextRequest(userId, kind, month, month.AddMonths(1).AddDays(-1));
            }

            var contextJson = AssistantContextBuilder.Serialize(await builder.BuildAsync(request));
            var promptVersion = AssistantPrompts.CurrentVersion(kind);
            var effort = kind == AssistantReviewKind.WeeklyScan ? AssistantEffort.Medium : AssistantEffort.High;
            var modelRequest = new AssistantModelRequest(
                AssistantPrompts.Load(promptVersion), contextJson, AssistantPrompts.OutputSchema(kind), effort);

            var client = new AnthropicAssistantModelClient(
                Options.Create(options), configuration, NullLogger<AnthropicAssistantModelClient>.Instance);

            var started = DateTime.UtcNow;
            AssistantModelResponse response;
            try
            {
                response = await client.GenerateAsync(modelRequest);
            }
            catch (Exception ex)
            {
                // El mensaje de la excepción propia es genérico; la causa técnica va a un archivo, sin la clave.
                var detail = (ex as AssistantModelException)?.Cause?.ToString() ?? ex.ToString();
                File.WriteAllText(Path.Combine(outDir, "live-error.txt"), detail.Replace(apiKey, "[clave]"), new UTF8Encoding(false));
                throw;
            }
            var seconds = (DateTime.UtcNow - started).TotalSeconds;

            var price = options.GetPrice(response.Model) ?? options.GetPrice(options.Model)!;
            var cost = AssistantUsageService.CalculateCost(price, response.Usage);
            var unverified = response.Text == null
                ? new List<string>()
                : FiguresOf(response.Text, contextJson);

            var label = $"{kind}-{request.PeriodStart:yyyy-MM}";
            File.WriteAllText(Path.Combine(outDir, $"live-{label}.context.json"), contextJson, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outDir, $"live-{label}.response.json"), response.Text ?? "", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outDir, $"live-{label}.summary.json"), JsonSerializer.Serialize(new
            {
                promptVersion,
                requestedModel = options.Model,
                answeredModel = response.Model,
                stopReason = response.StopReason,
                usage = response.Usage,
                costUsd = cost,
                seconds = Math.Round(seconds, 1),
                unverifiedFigures = unverified
            }, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), new UTF8Encoding(false));

            response.IsUsable.Should().BeTrue($"la respuesta no fue utilizable (stop_reason: {response.StopReason})");
        }

        private static List<string> FiguresOf(string responseText, string contextJson)
        {
            using var doc = JsonDocument.Parse(responseText);
            var parts = new List<string>();
            foreach (var name in new[] { "headline", "body" })
                if (doc.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    parts.Add(value.GetString()!);
            if (doc.RootElement.TryGetProperty("findings", out var findings) && findings.ValueKind == JsonValueKind.Array)
                foreach (var finding in findings.EnumerateArray())
                    foreach (var name in new[] { "title", "detail" })
                        if (finding.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                            parts.Add(value.GetString()!);
            if (doc.RootElement.TryGetProperty("highlights", out var highlights) && highlights.ValueKind == JsonValueKind.Array)
                parts.AddRange(highlights.EnumerateArray().Where(h => h.ValueKind == JsonValueKind.String).Select(h => h.GetString()!));
            return AssistantFigureChecker.FindUnverified(string.Join("\n", parts), contextJson).ToList();
        }

        private static string FindApiDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "JazFinanzasApp.API");
                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                    return candidate;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("No se encontró la carpeta JazFinanzasApp.API subiendo desde el directorio de ejecución.");
        }
    }
}
