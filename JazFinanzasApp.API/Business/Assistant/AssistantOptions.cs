namespace JazFinanzasApp.API.Business.Assistant
{
    // Configuración del asistente (sección "Assistant" de appsettings). La clave de la API NO va acá:
    // vive en Anthropic:ApiKey (App Service en producción, variable de entorno en local), nunca en
    // appsettings*.json (plan-devoluciones-llm.md, D4).
    public class AssistantOptions
    {
        public const string SectionName = "Assistant";

        public string Model { get; set; } = "claude-opus-5-5";

        // Techo de tokens de salida por llamada: la devolución más el razonamiento.
        public int MaxTokens { get; set; } = 16000;

        // Precio por millón de tokens, por modelo. Alimenta el cálculo de CostUsd y el tope mensual
        // (D11): si el modelo configurado no está acá, el servicio de uso debe fallar en vez de
        // registrar un costo en cero.
        public Dictionary<string, ModelPrice> Prices { get; set; } = new();

        public ModelPrice? GetPrice(string model) =>
            Prices.TryGetValue(model, out var price) ? price : null;
    }

    public class ModelPrice
    {
        public decimal InputPerMillion { get; set; }
        public decimal OutputPerMillion { get; set; }
        public decimal CacheReadPerMillion { get; set; }

        // Escritura de caché con TTL de 5 minutos (1,25 veces el precio de entrada).
        public decimal CacheWritePerMillion { get; set; }
    }
}
