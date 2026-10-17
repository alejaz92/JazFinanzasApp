namespace JazFinanzasApp.API.Business.Assistant
{
    // Una llamada al modelo. Las instrucciones van como system y el contexto serializado como único
    // mensaje del usuario; la respuesta se pide en un esquema fijo (plan-devoluciones-llm.md, D4).
    public record AssistantModelRequest(string SystemPrompt, string UserContent, string OutputSchemaJson, string Effort);

    // Esfuerzo de razonamiento: high para las devoluciones, medium para la revisión semanal (D4).
    public static class AssistantEffort
    {
        public const string High = "high";
        public const string Medium = "medium";
    }

    // Tokens que informa la API. InputTokens no incluye los de caché (la API los informa aparte).
    public record AssistantModelUsage(long InputTokens, long OutputTokens, long CacheReadTokens, long CacheWriteTokens)
    {
        public static AssistantModelUsage None { get; } = new(0, 0, 0, 0);
    }

    // Model es el que respondió de verdad, que puede ser el de respaldo si hubo una negativa.
    public record AssistantModelResponse(string Model, string? Text, string? StopReason, AssistantModelUsage Usage)
    {
        public const string RefusalStopReason = "refusal";
        public const string MaxTokensStopReason = "max_tokens";

        public bool IsRefusal => StopReason == RefusalStopReason;
        public bool IsTruncated => StopReason == MaxTokensStopReason;

        // Una negativa o un corte se pagan igual, pero no hay una devolución utilizable.
        public bool IsUsable => !IsRefusal && !IsTruncated && !string.IsNullOrWhiteSpace(Text);
    }

    // Único punto de contacto con la API de Anthropic. Los tests usan un falso: ninguno llama a la API
    // real (D1).
    public interface IAssistantModelClient
    {
        // Lanza AssistantSpendLimitException si Anthropic rechaza el pedido por límite de gasto o saldo
        // agotado, y AssistantModelException ante cualquier otra falla.
        Task<AssistantModelResponse> GenerateAsync(AssistantModelRequest request, CancellationToken cancellationToken = default);
    }
}
