using JazFinanzasApp.API.Business.DTO.TripReport;
using JazFinanzasApp.API.Business.Interfaces;

namespace JazFinanzasApp.API.Business.Assistant.Sources
{
    // Viajes (plan-devoluciones-llm.md, D2): los viajes cuyo rango de fechas toca el período, con cuánto se
    // gastó en ese período y el total del viaje entero. Usa ITripReportService, el mismo cálculo de Viajes →
    // General y Detalle: el total de un viaje ya viene neto de lo compartido.
    public class TripsContextSource : IAssistantContextSource
    {
        public const string SourceKey = "trips";

        private readonly ITripReportService _trips;
        private readonly IAssistantCurrencyResolver _currencies;

        public TripsContextSource(ITripReportService trips, IAssistantCurrencyResolver currencies)
        {
            _trips = trips;
            _currencies = currencies;
        }

        public string Key => SourceKey;

        public string Measures =>
            "Viajes cuyas fechas tocan el período. 'spentInPeriod' es lo gastado dentro del período (del día 1 al día de corte); 'total' y 'costPerDay' son " +
            "del viaje entero, hasta hoy, y ya vienen netos de lo que se reparte con otras personas. Un viaje puede tener gasto en el período aunque su total " +
            "sea mayor. Los gastos de un viaje también están en income-expense: no se suman a ese total.";

        public string PrimaryCurrency => "ARS";

        public async Task<object?> BuildAsync(AssistantContextRequest request)
        {
            var currencies = await _currencies.GetAsync();
            var from = request.Month;
            var toExclusive = request.Month.AddDays(request.CutoffDay);

            var arsTrips = (await _trips.GetGeneralAsync(request.UserId, currencies.Ars.Id)).ToList();
            var usdTrips = (await _trips.GetGeneralAsync(request.UserId, currencies.Usd.Id)).ToDictionary(t => t.TripId);

            var entries = new List<TripEntry>();
            foreach (var trip in arsTrips.Where(t => OverlapsPeriod(t, from, toExclusive)).OrderBy(t => t.StartDate))
            {
                usdTrips.TryGetValue(trip.TripId, out var usdTrip);
                var arsDetail = await _trips.GetDetailAsync(request.UserId, trip.TripId, currencies.Ars.Id);
                var usdDetail = await _trips.GetDetailAsync(request.UserId, trip.TripId, currencies.Usd.Id);

                entries.Add(new TripEntry(
                    trip.Name,
                    trip.Type,
                    trip.Status,
                    trip.StartDate.ToString("yyyy-MM-dd"),
                    trip.EndDate.ToString("yyyy-MM-dd"),
                    trip.DurationDays,
                    new PerCurrency<decimal>(AssistantFigures.RoundMoney(trip.Total, true), AssistantFigures.RoundMoney(usdTrip?.Total ?? 0m, false)),
                    new PerCurrency<decimal>(AssistantFigures.RoundMoney(trip.CostPerDay, true), AssistantFigures.RoundMoney(usdTrip?.CostPerDay ?? 0m, false)),
                    new PerCurrency<decimal>(
                        AssistantFigures.RoundMoney(SpentInPeriod(arsDetail, from, toExclusive), true),
                        AssistantFigures.RoundMoney(SpentInPeriod(usdDetail, from, toExclusive), false))));
            }

            return entries.Count == 0 ? null : new TripsData(entries);
        }

        // ── Pura: testeable sin mocks ─────────────────────────────────────────────────────────

        // El viaje toca el período si empieza antes de que termine y termina después de que empiece.
        public static bool OverlapsPeriod(TripGeneralReportDTO trip, DateTime from, DateTime toExclusive) =>
            trip.StartDate.Date < toExclusive && trip.EndDate.Date >= from;

        public static decimal SpentInPeriod(TripDetailReportDTO detail, DateTime from, DateTime toExclusive) =>
            detail.DailySpending.Where(d => d.Date >= from && d.Date < toExclusive).Sum(d => d.Amount);

        public record TripEntry(
            string Name, string Type, string Status, string StartDate, string EndDate, int DurationDays,
            PerCurrency<decimal> Total, PerCurrency<decimal> CostPerDay, PerCurrency<decimal> SpentInPeriod);

        public record TripsData(IReadOnlyList<TripEntry> Trips);
    }
}
