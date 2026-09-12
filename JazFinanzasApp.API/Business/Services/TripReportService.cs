using JazFinanzasApp.API.Business.DTO.TripReport;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    // Bloque E, Fase 21 (docs/plans/activos/plan-rediseno-reportes-v2.md): backend de Viajes (Flujo 6).
    // El Total reusa la misma fórmula que ReportService.GetTripsGeneralStatsAsync/GetTripDetailStatsAsync
    // (dos fuentes disjuntas: neto de Eventos vinculados al viaje + gastos propios etiquetados con
    // TripId, ver CLAUDE.md del backend, "Total de un Viaje: dos fuentes disjuntas") pero con `assetId`
    // explícito en vez de la moneda principal resuelta del usuario (T12, mismo criterio que
    // InvestmentReportService/CardReportService) — no se reusa ReportService directamente porque esos
    // métodos son privados y están atados a GetMainReferenceAssetIdAsync; se sigue el mismo patrón que
    // Fase 19/20a de duplicar el cálculo de conversión en el servicio nuevo en vez de generalizar el viejo.
    public class TripReportService : ITripReportService
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly ISharedEventRepository _sharedEventRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;

        public TripReportService(
            ITripRepository tripRepository,
            ITransactionRepository transactionRepository,
            ICardTransactionRepository cardTransactionRepository,
            ISharedEventRepository sharedEventRepository,
            IAssetRepository assetRepository,
            IAssetQuoteRepository assetQuoteRepository)
        {
            _tripRepository = tripRepository;
            _transactionRepository = transactionRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _sharedEventRepository = sharedEventRepository;
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
        }

        public async Task<IEnumerable<TripGeneralReportDTO>> GetGeneralAsync(int userId, int assetId)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var trips = await _tripRepository.GetByUserIdAsync(userId);

            var result = new List<TripGeneralReportDTO>();
            foreach (var trip in trips)
            {
                var (total, durationDays, costPerDay) = await ComputeTripCostAsync(trip, referenceAsset);
                result.Add(new TripGeneralReportDTO
                {
                    TripId = trip.Id,
                    Name = trip.Name,
                    Type = trip.Type,
                    StartDate = trip.StartDate,
                    EndDate = trip.EndDate,
                    Status = GetStatus(trip),
                    DurationDays = durationDays,
                    Total = total,
                    CostPerDay = costPerDay
                });
            }

            return result.OrderByDescending(r => r.StartDate).ToList();
        }

        public async Task<TripDetailReportDTO> GetDetailAsync(int userId, int tripId, int assetId)
        {
            var trip = await _tripRepository.GetByIdAsync(tripId)
                ?? throw new NotFoundException("Trip not found");
            if (trip.UserId != userId) throw new UnauthorizedDomainException();

            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var nets = await GetTripMovementNetsAsync(tripId, referenceAsset);
            var ownExpenses = await GetTripOwnExpensesAsync(tripId, referenceAsset);

            var total = SumTotal(nets, ownExpenses);
            var durationDays = GetDurationDays(trip.StartDate, trip.EndDate);
            var costPerDay = Math.Round(durationDays > 0 ? total / durationDays : total, 2);

            var combinedValues = nets
                .Select(n => new TripMovementValue { TransactionClass = n.TransactionClass, ValueInReference = n.ValueInReference, Date = n.Date })
                .Concat(ownExpenses)
                .ToList();

            var breakdown = combinedValues
                .GroupBy(v => v.TransactionClass ?? "Sin clase")
                .Select(g => new TripReportClassBreakdownDTO { TransactionClass = g.Key, Amount = Math.Round(g.Sum(v => v.ValueInReference), 2) })
                .OrderByDescending(b => b.Amount)
                .ToArray();

            // El neto por evento es solo de la fuente (1) — los gastos propios no pertenecen a ningún
            // evento — así que puede sumar menos que Total (mismo criterio que ReportService).
            var eventBreakdown = nets
                .GroupBy(n => new { n.EventId, n.EventName })
                .Select(g => new TripReportEventNetDTO { EventId = g.Key.EventId, EventName = g.Key.EventName, Amount = Math.Round(g.Sum(n => n.ValueInReference), 2) })
                .ToArray();

            var dailySpending = BuildDailySpending(trip.StartDate, trip.EndDate, combinedValues);

            // Ver el comentario de OutsideTripRangeAmount en el DTO: sin esto, un movimiento con fecha
            // fuera de [StartDate, EndDate] queda afuera del gráfico de día a día sin que se note.
            var outsideTripRangeAmount = Math.Round(combinedValues
                .Where(v => v.Date.Date < trip.StartDate.Date || v.Date.Date > trip.EndDate.Date)
                .Sum(v => v.ValueInReference), 2);

            var allTrips = await _tripRepository.GetByUserIdAsync(userId);
            var comparison = new List<TripCostPerDayComparisonDTO>();
            foreach (var t in allTrips)
            {
                var isCurrent = t.Id == tripId;
                var tCostPerDay = isCurrent ? costPerDay : (await ComputeTripCostAsync(t, referenceAsset)).CostPerDay;
                comparison.Add(new TripCostPerDayComparisonDTO
                {
                    TripId = t.Id,
                    Name = t.Name,
                    CostPerDay = tCostPerDay,
                    IsCurrent = isCurrent
                });
            }

            return new TripDetailReportDTO
            {
                TripId = tripId,
                Name = trip.Name,
                DurationDays = durationDays,
                Total = total,
                CostPerDay = costPerDay,
                Breakdown = breakdown,
                NetBreakdown = eventBreakdown,
                DailySpending = dailySpending,
                OutsideTripRangeAmount = outsideTripRangeAmount,
                CostPerDayComparison = comparison.OrderByDescending(c => c.CostPerDay).ToArray()
            };
        }

        private async Task<(decimal Total, int DurationDays, decimal CostPerDay)> ComputeTripCostAsync(Trip trip, Asset referenceAsset)
        {
            var nets = await GetTripMovementNetsAsync(trip.Id, referenceAsset);
            var ownExpenses = await GetTripOwnExpensesAsync(trip.Id, referenceAsset);
            var total = SumTotal(nets, ownExpenses);
            var durationDays = GetDurationDays(trip.StartDate, trip.EndDate);
            var costPerDay = Math.Round(durationDays > 0 ? total / durationDays : total, 2);
            return (total, durationDays, costPerDay);
        }

        private static decimal SumTotal(IEnumerable<TripMovementValue> nets, IEnumerable<TripMovementValue> ownExpenses)
            => Math.Round(nets.Sum(v => v.ValueInReference) + ownExpenses.Sum(v => v.ValueInReference), 2);

        // Inclusive de los dos extremos: Buenos Aires 2024 (3 días) y Bariloche 2026 (11 días) se
        // cargaron y se leyeron así en el relevamiento (1.6 del plan). Mínimo 1 para no dividir por cero.
        private static int GetDurationDays(DateTime start, DateTime end)
            => Math.Max((end.Date - start.Date).Days + 1, 1);

        private static TripDailySpendingDTO[] BuildDailySpending(DateTime start, DateTime end, IEnumerable<TripMovementValue> values)
        {
            var byDate = values
                .GroupBy(v => v.Date.Date)
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(v => v.ValueInReference), 2));

            var days = new List<TripDailySpendingDTO>();
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
                days.Add(new TripDailySpendingDTO { Date = date, Amount = byDate.GetValueOrDefault(date) });

            return days.ToArray();
        }

        private class TripMovementValue
        {
            public string? TransactionClass { get; set; }
            public decimal ValueInReference { get; set; }
            public DateTime Date { get; set; }
        }

        private class TripMovementNet : TripMovementValue
        {
            public int EventId { get; set; }
            public string EventName { get; set; } = string.Empty;
        }

        // Neto de Eventos Compartidos vinculados al viaje, movimiento por movimiento (la cotización
        // depende de la fecha de cada uno) — misma fórmula que SharedEventService.ComputeBalances para
        // la parte del usuario (Shares.Where(PersonId == null)).
        private async Task<List<TripMovementNet>> GetTripMovementNetsAsync(int tripId, Asset referenceAsset)
        {
            var events = await _sharedEventRepository.GetDetailByTripIdAsync(tripId);

            var result = new List<TripMovementNet>();
            foreach (var e in events)
            {
                foreach (var m in e.Movements ?? new List<SharedEventMovement>())
                {
                    var userAmount = m.Shares?.Where(s => s.PersonId == null).Sum(s => s.Amount) ?? 0;
                    if (userAmount == 0) continue;

                    var valueInUsd = await ToUsdAsync(m.AssetId, m.Asset, userAmount, m.Date);
                    var referenceQuote = await GetReferenceQuoteAsync(referenceAsset, m.Date);

                    result.Add(new TripMovementNet
                    {
                        TransactionClass = m.TransactionClass?.Description,
                        EventId = e.Id,
                        EventName = e.Name,
                        Date = m.Date,
                        ValueInReference = valueInUsd * referenceQuote
                    });
                }
            }

            return result;
        }

        // Fuente (2) del total de un viaje: los egresos etiquetados con TripId que no están ya
        // representados por el neto de los Eventos. Los repositorios se encargan de la exclusión.
        private async Task<List<TripMovementValue>> GetTripOwnExpensesAsync(int tripId, Asset referenceAsset)
        {
            var result = new List<TripMovementValue>();

            foreach (var t in await _transactionRepository.GetTripOwnExpenseTransactionsAsync(tripId))
            {
                var amount = Math.Abs(t.Amount);
                if (amount == 0) continue;

                var valueInUsd = t.QuotePrice is > 0
                    ? amount / t.QuotePrice.Value
                    : await ToUsdAsync(t.AssetId, t.Asset, amount, t.Date);

                result.Add(new TripMovementValue
                {
                    TransactionClass = t.TransactionClass?.Description,
                    Date = t.Date,
                    ValueInReference = valueInUsd * await GetReferenceQuoteAsync(referenceAsset, t.Date)
                });
            }

            // Los consumos de tarjeta no guardan cotización, así que se resuelve por la fecha del consumo.
            foreach (var ct in await _cardTransactionRepository.GetTripOwnExpenseCardTransactionsAsync(tripId))
            {
                var amount = Math.Abs(ct.TotalAmount);
                if (amount == 0) continue;

                var valueInUsd = await ToUsdAsync(ct.AssetId, ct.Asset, amount, ct.Date);

                result.Add(new TripMovementValue
                {
                    TransactionClass = ct.TransactionClass?.Description,
                    Date = ct.Date,
                    ValueInReference = valueInUsd * await GetReferenceQuoteAsync(referenceAsset, ct.Date)
                });
            }

            return result;
        }

        // USD no cotiza contra sí mismo; el resto se lleva a dólares con la cotización de la fecha.
        private async Task<decimal> ToUsdAsync(int assetId, Asset? asset, decimal amount, DateTime date)
        {
            if (asset?.Name == "Dolar Estadounidense") return amount;
            return amount / await _assetQuoteRepository.GetQuotePrice(assetId, date, "BLUE");
        }

        // USD es la moneda puente: no tiene cotización contra sí misma, se resuelve como identidad.
        private async Task<decimal> GetReferenceQuoteAsync(Asset referenceAsset, DateTime date)
        {
            if (referenceAsset.Symbol == "USD") return 1m;

            var type = referenceAsset.Symbol == "ARS" ? "BLUE" : "NA";
            return await _assetQuoteRepository.GetQuotePrice(referenceAsset.Id, date, type);
        }

        private static string GetStatus(Trip trip)
        {
            var today = DateTime.UtcNow.Date;
            if (today < trip.StartDate.Date) return "PLANNED";
            if (today > trip.EndDate.Date) return "FINISHED";
            return "IN_PROGRESS";
        }
    }
}
