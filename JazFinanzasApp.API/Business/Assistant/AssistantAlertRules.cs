using System.Globalization;
using JazFinanzasApp.API.Business.DTO.Dashboard;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Umbrales de las alertas por reglas (plan-devoluciones-llm.md, D8). Los valores salen de la calibración
    // de la Fase 9 sobre 25 meses de historia real (juego "Equilibrado"). Los montos mínimos son en pesos
    // nominales y se quedan cortos con la inflación: por eso viven en configuración, y se revisan en las
    // revisiones de calidad.
    public class AssistantAlertsOptions
    {
        public const string SectionName = "AssistantAlerts";

        // CategorySpike: gasto del mes en la categoría > Factor x su promedio mensual de los HistoryMonths
        // anteriores (los meses sin gasto cuentan como cero) y >= MinAmount.
        public decimal SpikeFactor { get; set; } = 2.0m;
        public decimal SpikeMinAmount { get; set; } = 150_000m;
        public int SpikeHistoryMonths { get; set; } = 6;

        // LargeMovement: egreso de los últimos RecentDays días > Factor x el MAYOR egreso individual de su
        // categoría en los LookbackMonths anteriores (con al menos MinPreviousCount movimientos previos) y
        // >= MinAmount. Comparar contra el mayor y no contra el promedio evita que el alquiler dispare la
        // alerta todos los meses (Fase 9).
        public decimal LargeFactor { get; set; } = 1.25m;
        public decimal LargeMinAmount { get; set; } = 200_000m;
        public int LargeLookbackMonths { get; set; } = 12;
        public int LargeMinPreviousCount { get; set; } = 3;
        public int LargeRecentDays { get; set; } = 7;

        // MissingIncome: categoría de ingreso con movimientos en >= MinMonthsWithIncome de los HistoryMonths
        // anteriores, sin ingreso este mes, pasados GraceDays días de su día habitual (la mediana).
        public int MissingIncomeMinMonths { get; set; } = 5;
        public int MissingIncomeHistoryMonths { get; set; } = 6;
        public int MissingIncomeGraceDays { get; set; } = 5;

        // Cuánta historia hay que pedirle a la base: la mayor de las ventanas, más el mes en curso.
        public int RequiredHistoryMonths =>
            Math.Max(Math.Max(SpikeHistoryMonths, LargeLookbackMonths), MissingIncomeHistoryMonths) + 1;
    }

    // Las tres reglas fijas del Flujo 2, como funciones puras: reciben los movimientos ya convertidos a pesos y
    // devuelven los avisos de la bandeja. Título y detalle los arma la app con una plantilla, nunca el modelo.
    public static class AssistantAlertRules
    {
        public const string CategorySpikeKind = "CategorySpike";
        public const string LargeMovementKind = "LargeMovement";
        public const string MissingIncomeKind = "MissingIncome";

        private static readonly CultureInfo Spanish = new("es-AR");

        // Todas las reglas, con el aviso de una categoría que ya tiene un egreso grande mostrado una sola vez:
        // si saltan los dos por la misma categoría, queda el del movimiento puntual, que es el más específico.
        public static List<DashboardPendingItemDTO> Evaluate(
            IReadOnlyCollection<AlertMovementResult> movements, DateTime today, AssistantAlertsOptions options)
        {
            var large = LargeMovements(movements, today, options);
            var largeCategories = large.Where(l => l.LinkId.HasValue).Select(l => l.LinkId!.Value).ToHashSet();
            var spikes = CategorySpikes(movements, today, options)
                .Where(s => !(s.LinkId.HasValue && largeCategories.Contains(s.LinkId.Value)));
            var missing = MissingIncome(movements, today, options);
            return spikes.Concat(large).Concat(missing).ToList();
        }

        public static List<DashboardPendingItemDTO> CategorySpikes(
            IReadOnlyCollection<AlertMovementResult> movements, DateTime today, AssistantAlertsOptions options)
        {
            var result = new List<DashboardPendingItemDTO>();
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var history = options.SpikeHistoryMonths;
            if (history <= 0) return result;
            var historyStart = monthStart.AddMonths(-history);

            foreach (var group in movements.Where(m => m.MovementType == "E" && m.Date >= historyStart && m.Date.Date <= today.Date).GroupBy(m => m.CategoryId))
            {
                var current = group.Where(m => m.Date >= monthStart).Sum(m => m.AmountInPesos);
                var previous = group.Where(m => m.Date < monthStart).Sum(m => m.AmountInPesos);
                var average = previous / history;
                // Sin historia no hay con qué comparar.
                if (average <= 0m) continue;
                if (current > options.SpikeFactor * average && current >= options.SpikeMinAmount)
                {
                    result.Add(new DashboardPendingItemDTO
                    {
                        Kind = CategorySpikeKind,
                        Title = group.First().CategoryName,
                        Detail = $"Va por encima de su promedio mensual de los últimos {history} meses ({Money(average)}).",
                        Amount = Math.Round(current, 0),
                        Severity = "warning",
                        LinkId = group.Key
                    });
                }
            }
            return result.OrderByDescending(r => r.Amount).ToList();
        }

        public static List<DashboardPendingItemDTO> LargeMovements(
            IReadOnlyCollection<AlertMovementResult> movements, DateTime today, AssistantAlertsOptions options)
        {
            var result = new List<DashboardPendingItemDTO>();
            var recentFrom = today.Date.AddDays(-(options.LargeRecentDays - 1));

            foreach (var group in movements.Where(m => m.MovementType == "E").GroupBy(m => m.CategoryId))
            {
                var ordered = group.OrderBy(m => m.Date).ToList();
                foreach (var movement in ordered.Where(m => m.Date.Date >= recentFrom && m.Date.Date <= today.Date))
                {
                    var lookbackStart = new DateTime(movement.Date.Year, movement.Date.Month, 1).AddMonths(-options.LargeLookbackMonths);
                    var previous = ordered.Where(p => p.Date >= lookbackStart && p.Date < movement.Date).Select(p => p.AmountInPesos).ToList();
                    if (previous.Count < options.LargeMinPreviousCount) continue;

                    var largestBefore = previous.Max();
                    if (movement.AmountInPesos > options.LargeFactor * largestBefore && movement.AmountInPesos >= options.LargeMinAmount)
                    {
                        var what = string.IsNullOrWhiteSpace(movement.Detail) ? "Un egreso" : Shorten(movement.Detail!.Trim(), 50);
                        result.Add(new DashboardPendingItemDTO
                        {
                            Kind = LargeMovementKind,
                            Title = group.First().CategoryName,
                            Detail = $"{what}: el mayor de la categoría en {options.LargeLookbackMonths} meses (el anterior fue de {Money(largestBefore)}).",
                            Amount = Math.Round(movement.AmountInPesos, 0),
                            Date = movement.Date,
                            Severity = "warning",
                            LinkId = group.Key
                        });
                    }
                }
            }
            return result.OrderByDescending(r => r.Amount).ToList();
        }

        public static List<DashboardPendingItemDTO> MissingIncome(
            IReadOnlyCollection<AlertMovementResult> movements, DateTime today, AssistantAlertsOptions options)
        {
            var result = new List<DashboardPendingItemDTO>();
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var history = options.MissingIncomeHistoryMonths;
            if (history <= 0) return result;
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            foreach (var group in movements.Where(m => m.MovementType == "I").GroupBy(m => m.CategoryId))
            {
                // Días del mes en que entró ingreso, por cada uno de los meses anteriores.
                var daysByMonth = new List<List<int>>();
                for (int k = 1; k <= history; k++)
                {
                    var start = monthStart.AddMonths(-k);
                    var days = group.Where(m => m.Date >= start && m.Date < start.AddMonths(1)).Select(m => m.Date.Day).OrderBy(d => d).ToList();
                    if (days.Count > 0) daysByMonth.Add(days);
                }
                if (daysByMonth.Count < options.MissingIncomeMinMonths) continue;

                // Día habitual: la mediana de las medianas mensuales (un mes con dos ingresos no lo corre).
                var usualDay = (int)Median(daysByMonth.Select(d => Median(d.Select(x => (double)x).ToList())).ToList());
                var checkDay = Math.Min(usualDay + options.MissingIncomeGraceDays, daysInMonth);

                // Avisa pasado el día de control, mientras no haya entrado nada este mes.
                if (today.Date <= new DateTime(today.Year, today.Month, checkDay)) continue;
                var arrivedThisMonth = group.Any(m => m.Date >= monthStart && m.Date.Date <= today.Date);
                if (arrivedThisMonth) continue;

                result.Add(new DashboardPendingItemDTO
                {
                    Kind = MissingIncomeKind,
                    Title = group.First().CategoryName,
                    Detail = $"Suele ingresar cerca del día {usualDay} y este mes todavía no hay ningún ingreso.",
                    Severity = "warning",
                    LinkId = group.Key
                });
            }
            return result.OrderBy(r => r.Title).ToList();
        }

        private static double Median(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
        }

        // "$1.234.567", con punto de miles y sin decimales.
        private static string Money(decimal value) => "$" + Math.Round(value, 0).ToString("N0", Spanish);

        private static string Shorten(string text, int max) => text.Length <= max ? text : text[..(max - 1)].TrimEnd() + "…";
    }
}
