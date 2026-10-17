using System.Text;
using System.Text.Json;
using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Infrastructure;
using JazFinanzasApp.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JazFinanzasApp.Tests.Manual
{
    // plan-devoluciones-llm.md, Fase 10 — corre las alertas por reglas del asistente sobre los datos reales de un
    // usuario y vuelca el resultado a un archivo, para compararlo con lo que anticipó la simulación de la Fase 9.
    // SOLO LECTURA: no levanta la aplicación (su arranque aplicaría migraciones) ni escribe nada. El usuario real
    // no tiene el asistente habilitado, así que solo salen las alertas por reglas.
    //
    // Variables de entorno:
    //   RUN_ASSISTANT_ALERTS=1             lo habilita
    //   ASSISTANT_ALERTS_OUT=<carpeta>     dónde escribir el resultado (nunca dentro del repo)
    //   ASSISTANT_ALERTS_USER_ID=<id>      usuario (por defecto 2, ajazmatie)
    //   ASSISTANT_ALERTS_DATE=yyyy-MM-dd   día a evaluar (por defecto hoy)
    public class AssistantAlertsRealDataTests
    {
        [ManualFact("RUN_ASSISTANT_ALERTS")]
        public async Task DumpTheRuleAlertsOfAUser()
        {
            var outDir = Environment.GetEnvironmentVariable("ASSISTANT_ALERTS_OUT");
            outDir.Should().NotBeNullOrWhiteSpace("hay que indicar dónde escribir (ASSISTANT_ALERTS_OUT)");
            Directory.CreateDirectory(outDir!);
            var userId = int.Parse(Environment.GetEnvironmentVariable("ASSISTANT_ALERTS_USER_ID") ?? "2");
            var dateText = Environment.GetEnvironmentVariable("ASSISTANT_ALERTS_DATE");
            var day = string.IsNullOrWhiteSpace(dateText) ? DateTime.Today : DateTime.ParseExact(dateText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

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
            services.AddSingleton<IConfiguration>(configuration); // el host real ya lo registra
            services.AddDbContext<ApplicationDbContext>(o => o
                .UseSqlServer(connectionString, s => s.CommandTimeout(120))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
            services.AddApplicationServices(configuration);

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var alerts = scope.ServiceProvider.GetRequiredService<IAssistantAlertService>();

            var items = await alerts.BuildAsync(userId, day);

            // Diagnóstico: las mismas reglas, directo y sin el resguardo del servicio, para ver cualquier error.
            var assets = scope.ServiceProvider.GetRequiredService<JazFinanzasApp.API.Infrastructure.Interfaces.IAssetRepository>();
            var transactions = scope.ServiceProvider.GetRequiredService<JazFinanzasApp.API.Infrastructure.Interfaces.ITransactionRepository>();
            var options = new AssistantAlertsOptions();
            var pesos = await assets.GetAssetByNameAsync("Peso Argentino");
            var from = new DateTime(day.Year, day.Month, 1).AddMonths(-options.RequiredHistoryMonths);
            var movements = (await transactions.GetAlertMovementsAsync(userId, pesos, from)).ToList();
            var direct = AssistantAlertRules.Evaluate(movements, day, pesos.Symbol, options);
            var report = new List<string>
            {
                $"movimientos={movements.Count} desde={from:yyyy-MM-dd} min={movements.Min(m => m.Date):yyyy-MM-dd} max={movements.Max(m => m.Date):yyyy-MM-dd} gastos={movements.Count(m => m.MovementType == 'E'.ToString())} ingresos={movements.Count(m => m.MovementType == 'I'.ToString())}",
                $"spike={AssistantAlertRules.CategorySpikes(movements, day, pesos.Symbol, options).Count} large={AssistantAlertRules.LargeMovements(movements, day, pesos.Symbol, options).Count} missing={AssistantAlertRules.MissingIncome(movements, day, options).Count} evaluate={direct.Count}"
            };
            report.AddRange(direct.Select(d => $"{d.Kind} | {d.Title} | {d.Detail}"));
            File.WriteAllLines(Path.Combine(outDir, $"diag-{userId}-{day:yyyy-MM-dd}.txt"), report);

            var json = JsonSerializer.Serialize(new { userId, date = day.ToString("yyyy-MM-dd"), items },
                new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            File.WriteAllText(Path.Combine(outDir, $"alerts-{userId}-{day:yyyy-MM-dd}.json"), json, new UTF8Encoding(false));
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
