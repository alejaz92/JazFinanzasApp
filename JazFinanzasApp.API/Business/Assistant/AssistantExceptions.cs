using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Falla del cliente del modelo (red, clave ausente o inválida, error del servicio). El mensaje se le
    // muestra al usuario, así que va en castellano y sin detalles técnicos; la causa queda en InnerException.
    public class AssistantModelException : BusinessRuleException
    {
        public const string DefaultMessage = "El asistente no pudo generar la devolución en este momento. Probá de nuevo más tarde.";

        public AssistantModelException(string? message = null, Exception? inner = null)
            : base(message ?? DefaultMessage)
        {
            Cause = inner;
        }

        // BusinessRuleException no acepta una excepción interna, así que la causa técnica se guarda acá.
        public Exception? Cause { get; }
    }

    // Anthropic rechazó el pedido por el límite de gasto del espacio de trabajo o por saldo agotado. Llega
    // como error 400 y no como 429 (guias/servicio-anthropic.md). Lo lanza el cliente; el servicio de uso
    // lo traduce a AssistantBudgetReachedException.
    public class AssistantSpendLimitException : Exception
    {
        public AssistantSpendLimitException(string providerMessage) : base(providerMessage) { }
    }

    // El asistente no puede generar más este mes: por el tope propio de la app (D11) o por el del lado de
    // Anthropic. Es un error de negocio: la devolución queda en Failed con este mensaje.
    public class AssistantBudgetReachedException : BusinessRuleException
    {
        public AssistantBudgetReachedException(string message) : base(message) { }
    }

    // Reconoce el 400 de Anthropic por límite de gasto o saldo agotado. El texto exacto lo define
    // Anthropic y puede cambiar: ante la duda un 400 se trata como falla genérica, que cuesta una
    // devolución fallida y no un tope mal declarado.
    public static class AssistantSpendLimitDetector
    {
        private static readonly string[] Markers =
        {
            "credit balance",
            "usage limit",
            "spend limit",
            "spending limit"
        };

        public static bool IsSpendLimitMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            return Markers.Any(m => message.Contains(m, StringComparison.OrdinalIgnoreCase));
        }
    }
}
