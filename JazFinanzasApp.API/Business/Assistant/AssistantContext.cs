using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Qué pasada del asistente se está armando y hasta qué día llegan los datos
    // (plan-devoluciones-llm.md, D2). Para MonthClose, PeriodStart es el primer día del mes y
    // CutoffDate su último día; para MidMonth y WeeklyScan, CutoffDate es el último día con datos
    // incluido (el día anterior a la generación).
    public record AssistantContextRequest(int UserId, string Kind, DateTime PeriodStart, DateTime CutoffDate)
    {
        public bool IsMonthClose => Kind == AssistantReviewKind.MonthClose;

        // Mes que se analiza. La revisión semanal mira el mes en curso al día de corte.
        public DateTime Month => Kind == AssistantReviewKind.WeeklyScan
            ? new DateTime(CutoffDate.Year, CutoffDate.Month, 1)
            : new DateTime(PeriodStart.Year, PeriodStart.Month, 1);

        public int DaysInMonth => DateTime.DaysInMonth(Month.Year, Month.Month);

        // Del día 1 al día de corte inclusive; el mes completo en un cierre.
        public int CutoffDay => IsMonthClose ? DaysInMonth : Math.Min(CutoffDate.Day, DaysInMonth);
    }

    // Una fuente de contexto: un área de la app que le pasa al asistente un resumen chico y serializable
    // de sus datos, calculado por los mismos servicios que usan los reportes. Sumar una fuente es una
    // clase y una línea de registro en DI (plan-devoluciones-llm.md, Flujo 3 y D2).
    public interface IAssistantContextSource
    {
        string Key { get; }

        // Una oración que dice qué mide la fuente. Viaja dentro del contexto para que el asistente no
        // mezcle mediciones distintas (D3).
        string Measures { get; }

        // Moneda con la que el texto abre esta fuente: ARS o USD. La otra se menciona cuando la lectura
        // cambia (D3).
        string PrimaryCurrency { get; }

        // Null si la fuente no tiene nada para este pedido: se saltea en el contexto.
        Task<object?> BuildAsync(AssistantContextRequest request);
    }

    public record AssistantPeriod(string Kind, string Month, string CutoffDate, int DaysInMonth, int DaysElapsed, bool IsCompleteMonth);

    public record AssistantContextSection(string Key, string Measures, string PrimaryCurrency, object Data);

    public record AssistantContext(AssistantPeriod Period, IReadOnlyList<AssistantContextSection> Sources);

    // Junta las fuentes registradas en un solo contexto. Si una fuente falla, el error sube: un
    // contexto con una fuente faltante haría que el asistente cuente el mes con un agujero sin saberlo.
    public class AssistantContextBuilder
    {
        // Sin escapar los acentos: el texto del contexto es castellano y los ó inflan los tokens.
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private readonly IReadOnlyList<IAssistantContextSource> _sources;

        public AssistantContextBuilder(IEnumerable<IAssistantContextSource> sources)
        {
            _sources = sources.ToList();
        }

        public async Task<AssistantContext> BuildAsync(AssistantContextRequest request)
        {
            var sections = new List<AssistantContextSection>();
            foreach (var source in _sources)
            {
                var data = await source.BuildAsync(request);
                if (data == null) continue;
                sections.Add(new AssistantContextSection(source.Key, source.Measures, source.PrimaryCurrency, data));
            }

            var period = new AssistantPeriod(
                request.Kind,
                request.Month.ToString("yyyy-MM"),
                request.CutoffDate.ToString("yyyy-MM-dd"),
                request.DaysInMonth,
                request.CutoffDay,
                request.IsMonthClose);

            return new AssistantContext(period, sections);
        }

        public static string Serialize(AssistantContext context) => JsonSerializer.Serialize(context, JsonOptions);
    }
}
