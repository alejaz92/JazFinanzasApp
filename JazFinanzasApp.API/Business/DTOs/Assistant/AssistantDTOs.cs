namespace JazFinanzasApp.API.Business.DTO.Assistant
{
    // GET status: si el asistente está habilitado y cuánto se gastó en el mes contra el tope
    // (plan-devoluciones-llm.md, D6 y D12). El frontend oculta la tarjeta de Inicio y el menú si Enabled es false.
    public class AssistantStatusDTO
    {
        public bool Enabled { get; set; }
        public decimal MonthlyBudgetUsd { get; set; }
        public decimal SpentUsd { get; set; }
        public DateTime RenewsOn { get; set; }
        public bool BudgetReached { get; set; }
    }

    // Una devolución completa, para la pantalla de detalle. No incluye el contexto que recibió el modelo:
    // pesa y no le sirve al usuario (queda en la base para las revisiones de calidad).
    public class AssistantReviewDTO
    {
        public int Id { get; set; }
        public string Kind { get; set; } = "";
        public DateTime PeriodStart { get; set; }
        // Último día con datos incluido ("datos hasta el 16/10").
        public DateTime CutoffDate { get; set; }
        // Ready / Failed / NoActivity (Pending solo mientras se genera).
        public string Status { get; set; } = "";
        public bool IsCurrent { get; set; }
        // User / Eval.
        public string Purpose { get; set; } = "";
        public string? Headline { get; set; }
        public List<string> Highlights { get; set; } = new();
        public string? Body { get; set; }
        // Las cifras del texto que no se encontraron en los datos ("cifras a revisar").
        public List<string> UnverifiedFigures { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? PromptVersion { get; set; }
        public string? Model { get; set; }
        public DateTime CreatedAt { get; set; }
        public AssistantFeedbackDTO? Feedback { get; set; }
    }

    // Una fila del historial, sin el cuerpo.
    public class AssistantReviewSummaryDTO
    {
        public int Id { get; set; }
        public string Kind { get; set; } = "";
        public DateTime PeriodStart { get; set; }
        public DateTime CutoffDate { get; set; }
        public string Status { get; set; } = "";
        public string? Headline { get; set; }
        public bool HasUnverifiedFigures { get; set; }
        public int? Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AssistantFeedbackDTO
    {
        // +1 útil / -1 no útil.
        public int Rating { get; set; }
        public bool HasWrongFigures { get; set; }
        public string? Comment { get; set; }
    }

    public static class AssistantLatestState
    {
        // Hay una devolución para mostrar.
        public const string Ready = "Ready";
        // Falta generar la que dice Kind y PeriodStart: el frontend dispara POST reviews/generate.
        public const string NotGenerated = "NotGenerated";
        // No falta nada, pero todavía no hay ninguna devolución con texto (los períodos no tuvieron movimientos).
        public const string Empty = "Empty";
    }

    // GET reviews/latest (D6): la última devolución, o cuál falta generar.
    public class AssistantLatestDTO
    {
        public string State { get; set; } = "";
        public string? MissingKind { get; set; }
        public DateTime? MissingPeriodStart { get; set; }
        public AssistantReviewDTO? Review { get; set; }
        // Solo cuando no falta ninguna devolución (D6, paso 3): conviene hacer la revisión semanal. El frontend la
        // dispara en segundo plano con POST reviews/generate (kind WeeklyScan), sin indicador.
        public bool WeeklyScanDue { get; set; }
    }

    // Un hallazgo del asistente, para la bandeja y para el historial de hallazgos.
    public class AssistantFindingDTO
    {
        public int Id { get; set; }
        // La pasada que lo encontró.
        public int ReviewId { get; set; }
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        // La categoría a la que se refiere, para el link.
        public int? TransactionClassId { get; set; }
        // Active / Dismissed / Expired.
        public string Status { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        // +1 útil / -1 no útil; null si no se calificó.
        public int? Rating { get; set; }
        public List<string> UnverifiedFigures { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateAssistantFindingDTO
    {
        // true: lo descarta (pasa a Dismissed).
        public bool? Dismiss { get; set; }
        // +1 útil / -1 no útil.
        public int? Rating { get; set; }
    }

    public class GenerateAssistantReviewDTO
    {
        // MonthClose, MidMonth o WeeklyScan (la revisión semanal solo corre cuando corresponde, salvo con Regenerate).
        public string Kind { get; set; } = "";
        // Primer día del mes. Si falta: el mes anterior para el cierre y el mes en curso para la mitad de mes.
        public DateTime? PeriodStart { get; set; }
        // true: genera una versión nueva aunque ya exista una (la anterior queda guardada).
        public bool Regenerate { get; set; }
    }

    public class SetAssistantFeedbackDTO
    {
        public int Rating { get; set; }
        public bool HasWrongFigures { get; set; }
        public string? Comment { get; set; }
    }
}
