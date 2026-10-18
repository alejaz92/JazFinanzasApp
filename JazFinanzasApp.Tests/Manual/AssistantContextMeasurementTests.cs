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

namespace JazFinanzasApp.Tests.Manual
{
    // Un test que solo corre si se lo pide: no forma parte de la suite de todos los días porque lee la
    // base real.
    public sealed class ManualFactAttribute : FactAttribute
    {
        public ManualFactAttribute(string environmentVariable)
        {
            if (Environment.GetEnvironmentVariable(environmentVariable) != "1")
                Skip = $"Manual: definir {environmentVariable}=1 para correrlo.";
        }
    }

    // plan-devoluciones-llm.md, Fase 4 — arma los contextos reales de un usuario y los vuelca a archivos para
    // medirlos. Es SOLO LECTURA: las fuentes de contexto solo consultan, y el test no levanta la aplicación
    // (su arranque aplicaría las migraciones pendientes sobre la base configurada) ni llama a SaveChanges.
    //
    // Variables de entorno:
    //   RUN_ASSISTANT_MEASUREMENT=1       lo habilita
    //   ASSISTANT_MEASURE_OUT=<carpeta>   dónde escribir los archivos (nunca dentro del repo)
    //   ASSISTANT_MEASURE_USER_ID=<id>    usuario a medir (por defecto 2, ajazmatie)
    //   ASSISTANT_MEASURE_EXTRA_CLOSES=yyyy-MM[,yyyy-MM]  cierres de mes adicionales (por ejemplo uno con un viaje)
    //   ASSISTANT_MEASURE_ONLY_EXTRA=1    mide solo los adicionales, sin los 6 cierres, la mitad de mes ni la semanal
    public class AssistantContextMeasurementTests
    {
        [ManualFact("RUN_ASSISTANT_MEASUREMENT")]
        public async Task DumpTheRealContextsOfAUser()
        {
            var outDir = Environment.GetEnvironmentVariable("ASSISTANT_MEASURE_OUT");
            outDir.Should().NotBeNullOrWhiteSpace("hay que indicar dónde escribir (ASSISTANT_MEASURE_OUT)");
            Directory.CreateDirectory(outDir!);
            var userId = int.Parse(Environment.GetEnvironmentVariable("ASSISTANT_MEASURE_USER_ID") ?? "2");

            var apiDirectory = FindApiDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiDirectory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
            var connectionString = configuration.GetConnectionString("JazFinanzasAppConnectionString");
            connectionString.Should().NotBeNullOrWhiteSpace();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(o => o
                .UseSqlServer(connectionString, s => s.CommandTimeout(120))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
            services.AddApplicationServices(configuration);

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var builder = scope.ServiceProvider.GetRequiredService<AssistantContextBuilder>();

            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var yesterday = today.AddDays(-1);
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

            var requests = new List<AssistantContextRequest>();
            if (Environment.GetEnvironmentVariable("ASSISTANT_MEASURE_ONLY_EXTRA") != "1")
            {
                for (int i = 1; i <= 6; i++)
                {
                    var month = thisMonth.AddMonths(-i);
                    requests.Add(new AssistantContextRequest(userId, AssistantReviewKind.MonthClose, month, month.AddMonths(1).AddDays(-1)));
                }
                requests.Add(new AssistantContextRequest(userId, AssistantReviewKind.MidMonth, thisMonth, yesterday));
                requests.Add(new AssistantContextRequest(userId, AssistantReviewKind.WeeklyScan, monday, yesterday));
            }

            foreach (var extra in (Environment.GetEnvironmentVariable("ASSISTANT_MEASURE_EXTRA_CLOSES") ?? string.Empty)
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var month = DateTime.ParseExact(extra, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
                requests.Add(new AssistantContextRequest(userId, AssistantReviewKind.MonthClose, month, month.AddMonths(1).AddDays(-1)));
            }

            var summary = new StringBuilder("context\tsource\tchars\tutf8_bytes\n");
            foreach (var request in requests)
            {
                var label = $"{request.Kind}-{request.PeriodStart:yyyy-MM-dd}";
                var context = await builder.BuildAsync(request);

                var full = AssistantContextBuilder.Serialize(context);
                File.WriteAllText(Path.Combine(outDir, $"{label}.json"), full, new UTF8Encoding(false));
                summary.AppendLine($"{label}\t(total)\t{full.Length}\t{Encoding.UTF8.GetByteCount(full)}");

                foreach (var section in context.Sources)
                {
                    var json = JsonSerializer.Serialize(section, AssistantContextBuilder.JsonOptions);
                    File.WriteAllText(Path.Combine(outDir, $"{label}.{section.Key}.json"), json, new UTF8Encoding(false));
                    summary.AppendLine($"{label}\t{section.Key}\t{json.Length}\t{Encoding.UTF8.GetByteCount(json)}");
                }
            }

            var summaryFile = Environment.GetEnvironmentVariable("ASSISTANT_MEASURE_ONLY_EXTRA") == "1" ? "summary-extra.tsv" : "summary.tsv";
            File.WriteAllText(Path.Combine(outDir, summaryFile), summary.ToString(), new UTF8Encoding(false));
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
