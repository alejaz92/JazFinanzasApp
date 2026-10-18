namespace JazFinanzasApp.API.Business.Assistant
{
    // Lo mismo en las dos monedas, calculado por separado: la variación en dólares no se deriva de la
    // de pesos (plan-devoluciones-llm.md, decisión 9 y D2).
    public record PerCurrency<T>(T Ars, T Usd);

    // Un monto contra su referencia, con todas las comparaciones ya hechas: el asistente nunca resta ni
    // calcula porcentajes (D2). El promedio es el de los meses anteriores y los meses sin movimiento
    // cuentan como cero. Los porcentajes son null cuando la base es cero o negativa.
    public record Comparison(
        decimal Current,
        decimal Previous,
        decimal Average,
        decimal DeltaVsPrevious,
        decimal? PctVsPrevious,
        decimal DeltaVsAverage,
        decimal? PctVsAverage);

    // Un valor contra el del mes anterior, para lo que no se promedia (un patrimonio, un valor de cartera):
    // el promedio de patrimonios de meses distintos no responde ninguna pregunta.
    public record ValueChange(decimal Current, decimal Previous, decimal Delta, decimal? PctVsPrevious);

    // Tasa de ahorro (resultado sobre ingresos, en %) y su variación en puntos porcentuales.
    public record RateComparison(
        decimal? Current,
        decimal? Previous,
        decimal? Average,
        decimal? DeltaPointsVsPrevious,
        decimal? DeltaPointsVsAverage);

    public static class AssistantFigures
    {
        // Pesos sin decimales, dólares con dos: el redondeo con el que viaja y se muestra cada monto.
        public static decimal RoundMoney(decimal value, bool isPesos) =>
            Math.Round(value, isPesos ? 0 : 2, MidpointRounding.AwayFromZero);

        public static decimal RoundPct(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

        // Variación porcentual de `value` contra `baseValue`; null si la base no es positiva.
        public static decimal? Pct(decimal value, decimal baseValue) =>
            baseValue > 0m ? RoundPct((value - baseValue) / baseValue * 100m) : null;

        public static Comparison Compare(decimal current, decimal previous, decimal average, bool isPesos) =>
            new(
                RoundMoney(current, isPesos),
                RoundMoney(previous, isPesos),
                RoundMoney(average, isPesos),
                RoundMoney(current - previous, isPesos),
                Pct(current, previous),
                RoundMoney(current - average, isPesos),
                Pct(current, average));

        public static ValueChange Change(decimal current, decimal previous, bool isPesos) =>
            new(
                RoundMoney(current, isPesos),
                RoundMoney(previous, isPesos),
                RoundMoney(current - previous, isPesos),
                Pct(current, previous));

        // Resultado sobre ingresos en %, o null si no hubo ingresos.
        public static decimal? Rate(decimal result, decimal income) =>
            income > 0m ? RoundPct(result / income * 100m) : null;

        public static RateComparison CompareRate(decimal? current, decimal? previous, decimal? average) =>
            new(
                current,
                previous,
                average,
                current.HasValue && previous.HasValue ? RoundPct(current.Value - previous.Value) : null,
                current.HasValue && average.HasValue ? RoundPct(current.Value - average.Value) : null);

        public static string ShortenDetail(string? detail, int maxLength = 80)
        {
            var text = (detail ?? string.Empty).Trim();
            return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
        }
    }
}
