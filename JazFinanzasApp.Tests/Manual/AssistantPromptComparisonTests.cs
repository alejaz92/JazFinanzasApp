using System.Text;
using System.Text.Json;
using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JazFinanzasApp.Tests.Manual
{
    // plan-devoluciones-llm.md, Fase 13 — el paso "regenerar y comparar" de una revisión de calidad
    // (docs/plans/guias/revision-asistente.md). Toma devoluciones ya guardadas de un usuario, las regenera con otra
    // versión de instrucciones SOBRE EL MISMO CONTEXTO QUE GUARDARON (D10: cambian las instrucciones, no los datos) y
    // deja lado a lado la versión vieja y la nueva en archivos, para armar el informe.
    //
    // Es el equivalente de POST assistant/reviews/{id}/preview para una sesión de trabajo con Claude, que no tiene el
    // login de la app. Diferencias con el endpoint:
    //   - SOLO LECTURA sobre la base: no escribe ninguna fila (el endpoint guarda una devolución Eval y su llamada).
    //     Por eso estas llamadas NO figuran en AssistantApiCalls: se suman a mano en la conciliación de la guía
    //     (el costo de cada una queda en el resumen).
    //   - Llama al cliente del modelo directamente, sin el servicio de uso: no pasa por el tope de la app. El que
    //     manda es el límite de gasto del espacio de trabajo y el saldo, y el tope de devoluciones de esta prueba.
    //
    // La clave se lee de un archivo y nunca se imprime ni se escribe en ningún resultado. Manda a Anthropic el
    // contexto real guardado de cada devolución (datos financieros del usuario).
    //
    // Variables de entorno:
    //   RUN_ASSISTANT_COMPARE=1                   lo habilita
    //   ASSISTANT_COMPARE_KEY_FILE=<archivo>      archivo con la clave, fuera de los repos y de OneDrive
    //   ASSISTANT_COMPARE_OUT=<carpeta>           dónde escribir (nunca dentro del repo)
    //   ASSISTANT_COMPARE_VERSION=v2              versión nueva de instrucciones ("v2" o "month-close.v2")
    //   ASSISTANT_COMPARE_USER_ID=<id>            usuario (por defecto 2, ajazmatie)
    //   ASSISTANT_COMPARE_REVIEW_IDS=1,2,3        devoluciones a comparar; sin esto, las vigentes y terminadas de los
    //                                             últimos 6 meses (cierres y mitades de mes)
    //   ASSISTANT_COMPARE_MAX=12                  tope de llamadas por corrida (por defecto 12, unos US$ 1,5)
    public class AssistantPromptComparisonTests
    {
        [ManualFact("RUN_ASSISTANT_COMPARE")]
        public async Task RegenerateSavedReviewsWithANewPromptVersion()
        {
            var keyFile = Environment.GetEnvironmentVariable("ASSISTANT_COMPARE_KEY_FILE");
            keyFile.Should().NotBeNullOrWhiteSpace("hay que indicar el archivo de la clave (ASSISTANT_COMPARE_KEY_FILE)");
            File.Exists(keyFile).Should().BeTrue("el archivo de la clave tiene que existir");
            var apiKey = File.ReadAllText(keyFile!).Trim();
            apiKey.Should().NotBeNullOrWhiteSpace();

            var outDir = Environment.GetEnvironmentVariable("ASSISTANT_COMPARE_OUT");
            outDir.Should().NotBeNullOrWhiteSpace("hay que indicar dónde escribir (ASSISTANT_COMPARE_OUT)");
            Directory.CreateDirectory(outDir!);

            var newVersion = Environment.GetEnvironmentVariable("ASSISTANT_COMPARE_VERSION");
            newVersion.Should().NotBeNullOrWhiteSpace("hay que indicar la versión nueva (ASSISTANT_COMPARE_VERSION, por ejemplo v2)");
            var userId = int.Parse(Environment.GetEnvironmentVariable("ASSISTANT_COMPARE_USER_ID") ?? "2");
            var max = int.Parse(Environment.GetEnvironmentVariable("ASSISTANT_COMPARE_MAX") ?? "12");
            var ids = (Environment.GetEnvironmentVariable("ASSISTANT_COMPARE_REVIEW_IDS") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse).ToList();

            var apiDirectory = FindApiDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiDirectory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddInMemoryCollection(new Dictionary<string, string?> { [AnthropicAssistantModelClient.ApiKeySetting] = apiKey })
                .Build();
            var connectionString = configuration.GetConnectionString("JazFinanzasAppConnectionString");
            connectionString.Should().NotBeNullOrWhiteSpace();

            var options = new AssistantOptions();
            configuration.GetSection(AssistantOptions.SectionName).Bind(options);

            // Solo lectura: una consulta sin seguimiento y nunca un SaveChanges.
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, s => s.CommandTimeout(120))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .Options;
            await using var context = new ApplicationDbContext(dbOptions);

            var query = context.AssistantReviews.Where(r => r.UserId == userId && r.Purpose == AssistantReviewPurpose.User
                && r.Status == AssistantReviewStatus.Ready && r.Kind != AssistantReviewKind.WeeklyScan && r.ContextJson != null);
            query = ids.Count > 0 ? query.Where(r => ids.Contains(r.Id)) : query.Where(r => r.IsCurrent);
            var reviews = (await query.ToListAsync()).OrderBy(r => r.PeriodStart).ThenBy(r => r.Kind).ToList();
            if (ids.Count == 0)
                reviews = reviews.Where(r => r.PeriodStart >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-6)).ToList();

            // Se compara de a un tipo por vez: las devoluciones de un tipo que no tiene esa versión de instrucciones se
            // saltean (queda anotado en el resumen) en vez de cortar la corrida.
            var skipped = reviews.Where(r => !AssistantPrompts.Exists(ResolveOrNull(r.Kind, newVersion!) ?? "")).ToList();
            reviews = reviews.Except(skipped).ToList();

            reviews.Should().NotBeEmpty("no hay devoluciones terminadas para comparar con esa versión de instrucciones");
            reviews.Count.Should().BeLessThanOrEqualTo(max, $"son más de {max} llamadas: se acota con ASSISTANT_COMPARE_REVIEW_IDS o se sube ASSISTANT_COMPARE_MAX a propósito");

            var client = new AnthropicAssistantModelClient(Options.Create(options), configuration, NullLogger<AnthropicAssistantModelClient>.Instance);
            var json = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var summary = new StringBuilder("reviewId\tkind\tperiod\toldVersion\tnewVersion\toldUnverified\tnewUnverified\tnewCostUsd\tseconds\tstopReason\n");
            decimal totalCost = 0m;
            foreach (var s in skipped)
                summary.AppendLine($"{s.Id}	{s.Kind}	{s.PeriodStart:yyyy-MM}	{s.PromptVersion}	sin {newVersion} para este tipo: se saltea					");

            foreach (var review in reviews)
            {
                var version = AssistantPrompts.ResolveVersion(review.Kind, newVersion!);
                var request = new AssistantModelRequest(AssistantPrompts.Load(version), review.ContextJson!, AssistantPrompts.OutputSchema(review.Kind), AssistantEffort.High);

                var started = DateTime.UtcNow;
                AssistantModelResponse response;
                try
                {
                    response = await client.GenerateAsync(request);
                }
                catch (Exception ex)
                {
                    // El mensaje de la excepción propia es genérico; la causa técnica va a un archivo, sin la clave.
                    var detail = (ex as AssistantModelException)?.Cause?.ToString() ?? ex.ToString();
                    File.WriteAllText(Path.Combine(outDir, $"compare-error-{review.Id}.txt"), detail.Replace(apiKey, "[clave]"), new UTF8Encoding(false));
                    File.WriteAllText(Path.Combine(outDir, "summary.tsv"), summary.ToString(), new UTF8Encoding(false));
                    throw;
                }
                var seconds = Math.Round((DateTime.UtcNow - started).TotalSeconds, 1);

                var price = options.GetPrice(response.Model) ?? options.GetPrice(options.Model)!;
                var cost = AssistantUsageService.CalculateCost(price, response.Usage);
                totalCost += cost;

                var newUnverified = response.Text == null ? new List<string>() : FiguresOf(response.Text, review.ContextJson!);
                var oldUnverified = string.IsNullOrEmpty(review.UnverifiedFiguresJson)
                    ? new List<string>() : JsonSerializer.Deserialize<List<string>>(review.UnverifiedFiguresJson)!;

                var label = $"{review.Kind}-{review.PeriodStart:yyyy-MM}-r{review.Id}";
                File.WriteAllText(Path.Combine(outDir, $"compare-{label}.json"), JsonSerializer.Serialize(new
                {
                    reviewId = review.Id,
                    kind = review.Kind,
                    period = review.PeriodStart.ToString("yyyy-MM"),
                    cutoffDate = review.CutoffDate.ToString("yyyy-MM-dd"),
                    old = new
                    {
                        promptVersion = review.PromptVersion,
                        model = review.Model,
                        headline = review.Headline,
                        highlights = string.IsNullOrEmpty(review.HighlightsJson) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(review.HighlightsJson),
                        body = review.Body,
                        unverifiedFigures = oldUnverified
                    },
                    @new = new
                    {
                        promptVersion = version,
                        model = response.Model,
                        stopReason = response.StopReason,
                        usable = response.IsUsable,
                        // El JSON tal cual lo devolvió el modelo: headline, highlights, body y findings.
                        response = response.Text == null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(response.Text),
                        unverifiedFigures = newUnverified,
                        costUsd = cost,
                        seconds
                    }
                }, json), new UTF8Encoding(false));

                summary.AppendLine($"{review.Id}\t{review.Kind}\t{review.PeriodStart:yyyy-MM}\t{review.PromptVersion}\t{version}\t{oldUnverified.Count}\t{newUnverified.Count}\t{cost}\t{seconds}\t{response.StopReason}");
            }

            summary.AppendLine($"TOTAL\t\t\t\t\t\t\t{totalCost}\t\t");
            File.WriteAllText(Path.Combine(outDir, "summary.tsv"), summary.ToString(), new UTF8Encoding(false));
        }

        // La versión completa ("month-close.v2") de un tipo, o null si ese tipo no tiene esa versión.
        private static string? ResolveOrNull(string kind, string version)
        {
            try { return AssistantPrompts.ResolveVersion(kind, version); }
            catch (JazFinanzasApp.API.Business.Exceptions.BusinessRuleException) { return null; }
        }

        // Las cifras del texto generado que no están en el contexto, igual que lo hace el servicio de devoluciones.
        private static List<string> FiguresOf(string responseText, string contextJson)
        {
            using var doc = JsonDocument.Parse(responseText);
            var parts = new List<string>();
            foreach (var name in new[] { "headline", "body" })
                if (doc.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    parts.Add(value.GetString()!);
            if (doc.RootElement.TryGetProperty("highlights", out var highlights) && highlights.ValueKind == JsonValueKind.Array)
                parts.AddRange(highlights.EnumerateArray().Where(h => h.ValueKind == JsonValueKind.String).Select(h => h.GetString()!));
            if (doc.RootElement.TryGetProperty("findings", out var findings) && findings.ValueKind == JsonValueKind.Array)
                foreach (var finding in findings.EnumerateArray())
                    foreach (var name in new[] { "title", "detail" })
                        if (finding.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                            parts.Add(value.GetString()!);
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
