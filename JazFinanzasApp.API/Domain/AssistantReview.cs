using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Una pasada del asistente: una devolución (cierre de mes o mitad de mes) o una revisión semanal
    // de hallazgos (plan-devoluciones-llm.md, Etapa 2 — Modelo de datos).
    //
    // Guarda todo lo necesario para reproducirla: el contexto exacto que recibió el modelo, la versión
    // de instrucciones y el modelo. Una regeneración agrega una fila nueva y apaga IsCurrent en la
    // anterior, así que las versiones quedan.
    public class AssistantReview : BaseEntity
    {
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        // MonthClose / MidMonth / WeeklyScan, ver AssistantReviewKind.
        [Required]
        [MaxLength(12)]
        public string Kind { get; set; }

        // Primer día del mes; para una WeeklyScan, el lunes de la semana.
        [Required]
        public DateTime PeriodStart { get; set; }

        // Último día con datos incluido: fin de mes para el cierre, el día anterior a la generación
        // para la mitad de mes y la revisión semanal.
        [Required]
        public DateTime CutoffDate { get; set; }

        // Pending / Ready / Failed / NoActivity, ver AssistantReviewStatus.
        [Required]
        [MaxLength(12)]
        public string Status { get; set; }

        public bool IsCurrent { get; set; }

        // User / Eval, ver AssistantReviewPurpose.
        [Required]
        [MaxLength(8)]
        public string Purpose { get; set; }

        // Lo generado. Una WeeklyScan no tiene texto propio, solo hallazgos.
        [MaxLength(300)]
        public string? Headline { get; set; }

        // Los tres puntos de la devolución, como JSON.
        public string? HighlightsJson { get; set; }

        // Cuerpo en markdown.
        public string? Body { get; set; }

        // Los datos exactos que recibió el modelo, para poder reproducir y comparar.
        public string? ContextJson { get; set; }

        [MaxLength(40)]
        public string? PromptVersion { get; set; }

        [MaxLength(60)]
        public string? Model { get; set; }

        // Cifras del texto que no se encontraron en el contexto (control de cifras, D7).
        public string? UnverifiedFiguresJson { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
    }

    public static class AssistantReviewKind
    {
        public const string MonthClose = "MonthClose";
        public const string MidMonth = "MidMonth";
        public const string WeeklyScan = "WeeklyScan";

        public static bool IsValid(string? value) =>
            value == MonthClose || value == MidMonth || value == WeeklyScan;
    }

    // NoActivity: el período no tuvo movimientos, no se llamó al modelo y no vuelve a intentarse
    // (plan-devoluciones-llm.md, D6). Es un estado agregado respecto del diagrama original de D6.
    public static class AssistantReviewStatus
    {
        public const string Pending = "Pending";
        public const string Ready = "Ready";
        public const string Failed = "Failed";
        public const string NoActivity = "NoActivity";

        public static bool IsValid(string? value) =>
            value == Pending || value == Ready || value == Failed || value == NoActivity;
    }

    // Eval: pasada hecha para comparar versiones de instrucciones en una revisión de calidad (D10);
    // no cuenta para el tope de gasto.
    public static class AssistantReviewPurpose
    {
        public const string User = "User";
        public const string Eval = "Eval";

        public static bool IsValid(string? value) => value == User || value == Eval;
    }
}
