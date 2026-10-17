using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Control de cifras (plan-devoluciones-llm.md, D7): cada número que aparece en el texto se busca entre
    // los números del contexto que recibió el modelo. Lo que no aparece se devuelve para guardarlo como
    // "cifras a revisar". No bloquea nada: es la red de seguridad de la regla de fondo (el asistente nunca
    // calcula).
    //
    // Qué revisa: montos ($85.000, US$ 32,94), porcentajes (18 %) y números de dos o más dígitos, en formato
    // argentino (punto de miles, coma decimal), con tolerancia de redondeo.
    // Qué no revisa: años, fechas ("16/10", "15 de octubre", "2026-09"), cantidades de tiempo ("6 meses")
    // ni expresiones como "el doble".
    public static class AssistantFigureChecker
    {
        private static readonly Regex NumberPattern = new(
            @"(?<![\p{L}\d.,])(?<int>\d{1,3}(?:\.\d{3})+|\d+)(?:,(?<dec>\d+))?(?![\p{L}\d])",
            RegexOptions.Compiled);

        private static readonly Regex MoneyPrefix = new(@"(?:US\$|u\$s|USD|\$)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PercentSuffix = new(@"^\s*(?:%|puntos?\s+porcentuales?|p\.?\s?p\.?(?![\p{L}]))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScaleSuffix = new(@"^\s+(?<word>millones|mill[oó]n|mil)(?![\p{L}])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TimeUnitSuffix = new(@"^\s+(?:meses|mes|d[ií]as|d[ií]a|semanas|semana|a[nñ]os|a[nñ]o)(?![\p{L}])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MonthSuffix = new(@"^\s+de\s+(?:enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)(?![\p{L}])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex AnyDigitRun = new(@"\d+", RegexOptions.Compiled);

        // Las cifras del texto que no se encontraron en el contexto, tal como están escritas, sin repetidos.
        public static IReadOnlyList<string> FindUnverified(string text, string contextJson) =>
            FindUnverified(text, ExtractContextNumbers(contextJson));

        public static IReadOnlyList<string> FindUnverified(string text, IReadOnlyCollection<decimal> contextNumbers)
        {
            var unverified = new List<string>();
            foreach (var figure in ExtractFigures(text))
            {
                var found = contextNumbers.Any(known => Math.Abs(known - figure.Value) <= figure.Tolerance);
                if (!found && !unverified.Contains(figure.Text))
                    unverified.Add(figure.Text);
            }
            return unverified;
        }

        // Todos los números del contexto, en valor absoluto: los numéricos del JSON y los que aparecen dentro
        // de textos (por ejemplo el detalle de una alerta, "$28.400 vence el 10").
        public static IReadOnlyCollection<decimal> ExtractContextNumbers(string contextJson)
        {
            var numbers = new HashSet<decimal>();
            using var doc = JsonDocument.Parse(contextJson);
            Collect(doc.RootElement, numbers);
            return numbers;
        }

        private static void Collect(JsonElement element, HashSet<decimal> numbers)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (element.TryGetDecimal(out var d)) numbers.Add(Math.Abs(d));
                    else numbers.Add(Math.Abs((decimal)element.GetDouble()));
                    break;
                case JsonValueKind.String:
                    CollectFromString(element.GetString() ?? "", numbers);
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray()) Collect(item, numbers);
                    break;
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject()) Collect(property.Value, numbers);
                    break;
            }
        }

        private static void CollectFromString(string text, HashSet<decimal> numbers)
        {
            foreach (Match m in NumberPattern.Matches(text))
                numbers.Add(Parse(m));
            // Fechas y códigos: "2026-10-15" aporta 2026, 10 y 15.
            foreach (Match m in AnyDigitRun.Matches(text))
                if (decimal.TryParse(m.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
                    numbers.Add(n);
        }

        private record Figure(string Text, decimal Value, decimal Tolerance);

        private static IEnumerable<Figure> ExtractFigures(string text)
        {
            foreach (Match m in NumberPattern.Matches(text))
            {
                var before = text[..m.Index];
                var after = text[(m.Index + m.Length)..];
                var intPart = m.Groups["int"].Value;
                var decPart = m.Groups["dec"].Success ? m.Groups["dec"].Value : "";
                var isMoney = MoneyPrefix.IsMatch(before);
                var percent = PercentSuffix.Match(after);
                var scaleMatch = ScaleSuffix.Match(after);

                if (IsDateOrYear(text, m, intPart, decPart, before, after)) continue;

                // Una cantidad de tiempo ("6 meses") no es una cifra de la app, salvo que sea un monto o un porcentaje.
                if (!isMoney && !percent.Success && TimeUnitSuffix.IsMatch(after)) continue;

                // Sin símbolo, solo se revisan los números de dos o más dígitos o con decimales.
                var digitCount = intPart.Replace(".", "").Length;
                if (!isMoney && !percent.Success && digitCount < 2 && decPart.Length == 0) continue;

                var value = Parse(m);
                var scale = 1m;
                if (scaleMatch.Success)
                    scale = scaleMatch.Groups["word"].Value.StartsWith("mill", StringComparison.OrdinalIgnoreCase) ? 1_000_000m : 1_000m;
                value *= scale;

                var tolerance = Tolerance(intPart, decPart, scale);

                var end = m.Index + m.Length;
                if (percent.Success) end += percent.Length;
                else if (scaleMatch.Success) end += scaleMatch.Length;
                var start = m.Index;
                var prefix = MoneyPrefix.Match(before);
                if (prefix.Success) start = prefix.Index;
                yield return new Figure(text[start..end].Trim(), value, tolerance);
            }
        }

        // Media unidad del último dígito escrito. Un entero grande redondeado ("$85.000") se toma como
        // redondeado a la posición de su último dígito distinto de cero, hasta los miles: $85.000 acepta 85.432.
        private static decimal Tolerance(string intPart, string decPart, decimal scale)
        {
            if (decPart.Length > 0)
                return 0.5m * Pow10Negative(decPart.Length) * scale;

            var digits = intPart.Replace(".", "");
            var value = decimal.Parse(digits, CultureInfo.InvariantCulture);
            if (scale == 1m && value >= 1000m)
            {
                var trailingZeros = digits.Length - digits.TrimEnd('0').Length;
                return 0.5m * Pow10(Math.Min(trailingZeros, 3));
            }
            return 0.5m * scale;
        }

        private static decimal Pow10(int n) { var r = 1m; for (var i = 0; i < n; i++) r *= 10m; return r; }
        private static decimal Pow10Negative(int n) { var r = 1m; for (var i = 0; i < n; i++) r /= 10m; return r; }

        private static decimal Parse(Match m)
        {
            var intPart = m.Groups["int"].Value.Replace(".", "");
            var dec = m.Groups["dec"].Success ? "." + m.Groups["dec"].Value : "";
            return decimal.Parse(intPart + dec, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        private static bool IsDateOrYear(string text, Match m, string intPart, string decPart, string before, string after)
        {
            // 16/10, 16/10/2026
            if (after.StartsWith('/') && after.Length > 1 && char.IsDigit(after[1])) return true;
            if (before.EndsWith('/') && before.Length > 1 && char.IsDigit(before[^2])) return true;
            // 2026-09, 2026-09-30
            if (after.StartsWith('-') && after.Length > 1 && char.IsDigit(after[1])) return true;
            if (before.EndsWith('-') && before.Length > 1 && char.IsDigit(before[^2])) return true;
            // "15 de octubre"
            if (MonthSuffix.IsMatch(after)) return true;
            // años sueltos
            if (decPart.Length == 0 && !intPart.Contains('.') && intPart.Length == 4
                && int.TryParse(intPart, out var year) && year is >= 1900 and <= 2100
                && !MoneyPrefix.IsMatch(before) && !PercentSuffix.IsMatch(after))
                return true;
            return false;
        }
    }
}
