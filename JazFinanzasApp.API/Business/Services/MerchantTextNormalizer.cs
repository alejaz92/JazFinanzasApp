using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JazFinanzasApp.API.Business.Services
{
    // Normalización de detalle → texto comparable (T7, plan-rediseno-reportes.md). Puro y sin
    // acceso a datos, para poder testearlo aislado del resolver — mismo espíritu que
    // SplitwiseCsvParser.
    //
    // El agrupamiento de "variantes del mismo comercio" (D-2a) se resuelve por **coincidencia
    // exacta del texto ya normalizado**, no por similitud difusa (distancia de edición, etc.):
    // sacar números y palabras vacías del dominio ya alcanza para el caso real del plan
    // ("Coto", "COTO 3456", "compra coto" → los tres normalizan a "coto"). Una heurística de
    // similitud aproximada sumaría falsos positivos difíciles de explicarle al usuario, para un
    // beneficio que el caso de uso real no pide — D-2a ya acepta que el resultado inicial sea
    // imperfecto y mejore con las correcciones manuales, no que el algoritmo sea perfecto.
    public static class MerchantTextNormalizer
    {
        // "compra"/"pago"/"débito"/"tarjeta" del enunciado de T7, más las preposiciones/artículos
        // más comunes en los detalles reales (evita que "compra en farmacia" y "farmacia" difieran
        // solo por la preposición).
        private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
        {
            "compra", "pago", "pagos", "debito", "credito", "tarjeta", "transferencia",
            "de", "del", "la", "el", "los", "las", "en", "a", "con", "y"
        };

        private static readonly Regex NonLetters = new(@"[^a-z\s]", RegexOptions.Compiled);
        private static readonly Regex MultipleSpaces = new(@"\s+", RegexOptions.Compiled);

        public static string Normalize(string? detail)
        {
            if (string.IsNullOrWhiteSpace(detail)) return string.Empty;

            var lower = detail.ToLowerInvariant();
            var withoutAccents = RemoveDiacritics(lower);
            var lettersOnly = NonLetters.Replace(withoutAccents, " ");

            var words = MultipleSpaces.Split(lettersOnly)
                .Where(w => w.Length > 0 && !StopWords.Contains(w));

            return string.Join(' ', words).Trim();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
