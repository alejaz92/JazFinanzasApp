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
        public const string WeeklyScanCurrent = "weekly-scan.v1";

        private const string ResourcePrefix = "JazFinanzasApp.API.Business.Assistant.Prompts.";

        // Versión vigente de las instrucciones de un tipo de pasada.
        public static string CurrentVersion(string kind) => kind switch
        {
            AssistantReviewKind.MonthClose => MonthCloseCurrent,
            AssistantReviewKind.MidMonth => MidMonthCurrent,
            AssistantReviewKind.WeeklyScan => WeeklyScanCurrent,
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

        // Esquema de la respuesta estructurada de una pasada (D4): la devolución trae titular, tres puntos,
        // cuerpo y hasta tres hallazgos; la revisión semanal, solo los hallazgos.
        public static string OutputSchema(string kind) =>
            kind == AssistantReviewKind.WeeklyScan ? WeeklyScanOutputSchema : ReviewOutputSchema;

        // Tipos de hallazgo. Son parte del Fingerprint (D15): el mismo tipo sobre la misma categoría, en el mismo
        // mes, no se repite.
        public static readonly string[] FindingKinds = { "unusual-amount", "trend", "missing-data", "other" };

        private const string FindingSchema = """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "detail": { "type": "string" },
                "category": { "type": ["string", "null"] },
                "kind": { "type": "string", "enum": ["unusual-amount", "trend", "missing-data", "other"] }
              },
              "required": ["title", "detail", "category", "kind"],
              "additionalProperties": false
            }
            """;

        public static readonly string ReviewOutputSchema = $$"""
            {
              "type": "object",
              "properties": {
                "headline": { "type": "string" },
                "highlights": { "type": "array", "items": { "type": "string" } },
                "body": { "type": "string" },
                "findings": { "type": "array", "items": {{FindingSchema}} }
              },
              "required": ["headline", "highlights", "body", "findings"],
              "additionalProperties": false
            }
            """;

        public static readonly string WeeklyScanOutputSchema = $$"""
            {
              "type": "object",
              "properties": {
                "findings": { "type": "array", "items": {{FindingSchema}} }
              },
              "required": ["findings"],
              "additionalProperties": false
            }
            """;
    }
}
