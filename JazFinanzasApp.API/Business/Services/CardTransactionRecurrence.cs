using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Business.Services
{
    // Meses en que un consumo recurrente de tarjeta estuvo activo (docs/plans/activos/plan-devengado-recurrentes.md, D1).
    // Un recurrente es una cadena de tramos de precio: cambiar el precio cierra el tramo vigente ("YES" pasa a "CLOSED",
    // con LastInstallment en el mes anterior al cambio) y abre uno nuevo. Cada tramo cobra InstallmentAmount en cada mes
    // de su rango, así que "YES" y "CLOSED" se tratan igual — solo cambia si el rango tiene fin o no.
    // Las compras "NO" (de un pago o en cuotas fijas) no pasan por acá.
    public static class CardTransactionRecurrence
    {
        public static bool IsRecurring(CardTransaction ct) => ct.Repeat == "YES" || ct.Repeat == "CLOSED";

        public static DateTime FirstMonth(CardTransaction ct) => new DateTime(ct.FirstInstallment.Year, ct.FirstInstallment.Month, 1);

        // null = sin fin. Un "YES" ignora LastInstallment, sea cual sea su valor: hay vigentes con 0001-01-01 en vez de
        // null (D4). Un "CLOSED" sin fin válido (nulo, o anterior al primer mes) cuenta solo su primer mes (D3): no hay
        // forma de saber cuánto duró, y estirarlo hasta hoy inventaría meses.
        public static DateTime? LastMonth(CardTransaction ct)
        {
            if (ct.Repeat == "YES") return null;

            var firstMonth = FirstMonth(ct);
            if (ct.LastInstallment == null || ct.LastInstallment.Value < firstMonth) return firstMonth;
            return new DateTime(ct.LastInstallment.Value.Year, ct.LastInstallment.Value.Month, 1);
        }

        public static bool IsActiveIn(CardTransaction ct, DateTime month)
        {
            var lastMonth = LastMonth(ct);
            return month >= FirstMonth(ct) && (lastMonth == null || month <= lastMonth.Value);
        }

        // Cantidad de meses activos dentro de [startMonth, endMonth], ambos primer día de mes.
        public static int CountActiveMonthsBetween(CardTransaction ct, DateTime startMonth, DateTime endMonth)
        {
            var firstMonth = FirstMonth(ct);
            var lastMonth = LastMonth(ct);
            var effectiveStart = firstMonth > startMonth ? firstMonth : startMonth;
            var effectiveEnd = lastMonth != null && lastMonth.Value < endMonth ? lastMonth.Value : endMonth;
            if (effectiveStart > effectiveEnd) return 0;
            return (effectiveEnd.Year - effectiveStart.Year) * 12 + effectiveEnd.Month - effectiveStart.Month + 1;
        }
    }
}
