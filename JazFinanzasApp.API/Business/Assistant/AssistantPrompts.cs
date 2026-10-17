using System.Reflection;
using System.Text.RegularExpressions;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Instrucciones versionadas del asistente (plan-devoluciones-llm.md, D5). Cada versión es un archivo .md
    // embebido: cambiar las instrucciones es un archivo nuevo (v2) más el cambio de la constante vigente, con
    // deploy. Cada generación guarda la versión que usó, en el formato "month-close.v1".
    public static class AssistantPrompts
    {
        public const string MonthCloseCurrent = "month-close.v1";
        public const string MidMonthCurrent = "mid-month.v1";

        private const string ResourcePrefix = "JazFinanzasApp.API.Business.Assistant.Prompts.";

        // Versión vigente de las instrucciones de un tipo de pasada.
        public static string CurrentVersion(string kind) => kind switch
        {
            AssistantReviewKind.MonthClose => MonthCloseCurrent,
            AssistantReviewKind.MidMonth => MidMonthCurrent,
            _ => throw new BusinessRuleException("Ese tipo de pasada todavía no tiene instrucciones.")
        };

        // El nombre base del archivo de un tipo ("month-close"), para armar versiones como "month-close.v2".
        public static string BaseName(string kind) => CurrentVersion(kind).Split('.')[0];

        // De "v2" a "month-close.v2". Acepta también el nombre completo.
        public static string ResolveVersion(string kind, string promptVersion)
        {
            var requested = (promptVersion ?? "").Trim();
            if (requested.Length == 0)
                throw new BusinessRuleException("Falta indicar la versión de instrucciones.");

            var full = Regex.IsMatch(requested, @"^v\d+$") ? $"{BaseName(kind)}.{requested}" : requested;
            if (!full.StartsWith(BaseName(kind) + ".", StringComparison.Ordinal) || !Exists(full))
                throw new BusinessRuleException($"No existe la versión de instrucciones '{promptVersion}' para este tipo de devolución.");
            return full;
        }

        public static bool Exists(string version) => FindResourceName(version) != null;

        public static string Load(string version)
        {
            var name = FindResourceName(version)
                ?? throw new BusinessRuleException($"No existe la versión de instrucciones '{version}'.");
            using var stream = typeof(AssistantPrompts).Assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string? FindResourceName(string version)
        {
            if (!Regex.IsMatch(version ?? "", @"^[a-z\-]+\.v\d+$")) return null;
            var name = $"{ResourcePrefix}{version}.md";
            return typeof(AssistantPrompts).Assembly.GetManifestResourceNames().FirstOrDefault(n => n == name);
        }

        // Esquema de la respuesta estructurada de una devolución (D4). `findings` se suma en la Fase 8.
        public const string ReviewOutputSchema = """
            {
              "type": "object",
              "properties": {
                "headline": { "type": "string" },
                "highlights": { "type": "array", "items": { "type": "string" } },
                "body": { "type": "string" }
              },
              "required": ["headline", "highlights", "body"],
              "additionalProperties": false
            }
            """;
    }
}
