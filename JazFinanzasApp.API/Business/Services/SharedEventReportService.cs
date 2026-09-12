using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.DTO.SharedEventReport;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    // Bloque E, Fase 21 (docs/plans/activos/plan-rediseno-reportes-v2.md): backend de Compartidos
    // (Flujo 7). "Por evento" no suma nada acá: ya está resuelto por SharedEventService.GetByIdAsync
    // (Balances, CategoryTotals, Movements, Payments), que es exactamente "quién puso qué, cómo se
    // repartió, qué quedó pendiente" -- este servicio cubre General (Balances actuales + evolución +
    // ranking) y Por persona (lo mismo, más el historial cruzando eventos).
    public class SharedEventReportService : ISharedEventReportService
    {
        private readonly ISharedEventRepository _sharedEventRepository;
        private readonly ISharedEventService _sharedEventService;
        private readonly IPersonRepository _personRepository;

        public SharedEventReportService(
            ISharedEventRepository sharedEventRepository,
            ISharedEventService sharedEventService,
            IPersonRepository personRepository)
        {
            _sharedEventRepository = sharedEventRepository;
            _sharedEventService = sharedEventService;
            _personRepository = personRepository;
        }

        public async Task<SharedEventGeneralReportDTO> GetGeneralAsync(int userId)
        {
            var balances = (await _sharedEventService.GetConsolidatedDebtsAsync(userId)).ToList();
            var events = await _sharedEventRepository.GetAllDetailByUserIdAsync(userId);

            var balanceEvolution = BuildBalanceEvolution(events, personId: null, negate: false);

            // Ranking nominal: si un evento mezclara monedas se suma igual para ordenar (no para
            // mostrar un total financiero, eso lo hace Amounts por separado) -- en la práctica los
            // 3 eventos de 1.6 son de una sola moneda cada uno.
            var ranking = events
                .Select(e => new SharedEventRankingDTO
                {
                    EventId = e.Id,
                    EventName = e.Name,
                    IsClosed = e.IsClosed,
                    Amounts = (e.Movements ?? new List<SharedEventMovement>())
                        .GroupBy(m => m.AssetId)
                        .Select(g => new SharedEventAssetAmountDTO
                        {
                            AssetId = g.Key,
                            AssetName = g.First().Asset?.Name ?? string.Empty,
                            AssetSymbol = g.First().Asset?.Symbol ?? string.Empty,
                            Total = Math.Round(g.Sum(m => m.TotalAmount), 2)
                        })
                        .OrderByDescending(a => a.Total)
                        .ToList()
                })
                .OrderByDescending(r => r.Amounts.Sum(a => a.Total))
                .ToList();

            return new SharedEventGeneralReportDTO
            {
                Balances = balances,
                BalanceEvolution = balanceEvolution,
                EventRanking = ranking
            };
        }

        public async Task<SharedEventPersonReportDTO> GetByPersonAsync(int userId, int personId)
        {
            var person = await _personRepository.GetByIdAsync(personId)
                ?? throw new NotFoundException("Persona no encontrada");
            if (person.UserId != userId) throw new UnauthorizedDomainException();

            var balances = (await _sharedEventService.GetConsolidatedDebtsAsync(userId))
                .Where(b => b.PersonId == personId)
                .ToList();

            var events = await _sharedEventRepository.GetAllDetailByUserIdAsync(userId);

            // Signo invertido respecto de GetGeneralAsync: acá "positivo" tiene que leerse igual que
            // Balances (PendingInFavor - PendingAgainst, "me debe"), y el neto de ComputeBalances para
            // un tercero es al revés de eso (positivo ahí = esa persona puso más de lo que consumió).
            var balanceEvolution = BuildBalanceEvolution(events, personId, negate: true);

            var personMovements = events
                .SelectMany(e => (e.Movements ?? new List<SharedEventMovement>())
                    .Where(m => m.PayerPersonId == personId || (m.Shares?.Any(s => s.PersonId == personId) ?? false))
                    .Select(m => (Event: e, Movement: m)))
                .ToList();

            var categoryTotals = personMovements
                .Select(x => new
                {
                    x.Movement.AssetId,
                    AssetName = x.Movement.Asset?.Name ?? string.Empty,
                    AssetSymbol = x.Movement.Asset?.Symbol ?? string.Empty,
                    x.Movement.TransactionClassId,
                    TransactionClassName = x.Movement.TransactionClass?.Description ?? string.Empty,
                    // La parte de esta persona, no el total del movimiento (mismo criterio que
                    // ComputeCategoryTotals usa TotalAmount para el evento entero).
                    Amount = x.Movement.Shares?.Where(s => s.PersonId == personId).Sum(s => s.Amount) ?? 0
                })
                .GroupBy(x => (x.AssetId, x.TransactionClassId))
                .Select(g => new SharedEventCategoryTotalDTO
                {
                    AssetId = g.Key.AssetId,
                    AssetName = g.First().AssetName,
                    AssetSymbol = g.First().AssetSymbol,
                    TransactionClassId = g.Key.TransactionClassId,
                    TransactionClassName = g.First().TransactionClassName,
                    Total = Math.Round(g.Sum(x => x.Amount), 2)
                })
                .Where(c => c.Total != 0)
                .OrderByDescending(c => c.Total)
                .ToList();

            var movements = personMovements
                .Select(x => new SharedEventPersonMovementDTO
                {
                    EventId = x.Event.Id,
                    EventName = x.Event.Name,
                    Movement = SharedEventService.MapMovementToDTO(x.Movement)
                })
                .OrderByDescending(x => x.Movement.Date)
                .ToList();

            var payments = events
                .SelectMany(e => (e.Payments ?? new List<SharedEventPayment>())
                    .Where(p => p.FromPersonId == personId || p.ToPersonId == personId)
                    .Select(p => new SharedEventPersonPaymentDTO
                    {
                        EventId = e.Id,
                        EventName = e.Name,
                        Payment = SharedEventService.MapPaymentToDTO(p)
                    }))
                .OrderByDescending(x => x.Payment.Date)
                .ToList();

            return new SharedEventPersonReportDTO
            {
                PersonId = personId,
                PersonName = person.Alias ?? person.Name,
                Balances = balances,
                BalanceEvolution = balanceEvolution,
                CategoryTotals = categoryTotals,
                Movements = movements,
                Payments = payments
            };
        }

        // Una serie mensual por moneda, desde el primer movimiento/pago de esa moneda hasta el mes
        // actual -- "arranca corta y se va llenando sola" (Flujo 7). `negate` es para Por persona
        // (ver el comentario en GetByPersonAsync).
        private static List<SharedEventBalancePointDTO> BuildBalanceEvolution(List<SharedEvent> events, int? personId, bool negate)
        {
            var assetActivity = events
                .SelectMany(e => (e.Movements ?? new List<SharedEventMovement>())
                    .Select(m => (AssetId: m.AssetId, Symbol: m.Asset?.Symbol ?? string.Empty, Date: m.Date))
                    .Concat((e.Payments ?? new List<SharedEventPayment>())
                        .Select(p => (AssetId: p.AssetId, Symbol: p.Asset?.Symbol ?? string.Empty, Date: p.Date))))
                .GroupBy(x => x.AssetId)
                .ToDictionary(g => g.Key, g => (Symbol: g.First().Symbol, FirstDate: g.Min(x => x.Date)));

            var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var result = new List<SharedEventBalancePointDTO>();

            foreach (var (assetId, info) in assetActivity)
            {
                var monthCursor = new DateTime(info.FirstDate.Year, info.FirstDate.Month, 1);
                while (monthCursor <= currentMonthStart)
                {
                    var monthEnd = monthCursor.AddMonths(1).AddDays(-1);
                    var balance = ComputeNetBalanceAsOf(events, personId, assetId, monthEnd);

                    result.Add(new SharedEventBalancePointDTO
                    {
                        Month = monthCursor,
                        AssetId = assetId,
                        AssetSymbol = info.Symbol,
                        MyBalance = negate ? -balance : balance
                    });

                    monthCursor = monthCursor.AddMonths(1);
                }
            }

            return result.OrderBy(p => p.AssetId).ThenBy(p => p.Month).ToList();
        }

        // Misma fórmula que SharedEventService.ComputeBalances (Contributed - Consumed + pagos), pero
        // filtrando movimientos y pagos a Date <= asOf para poder reconstruir un mes pasado.
        private static decimal ComputeNetBalanceAsOf(List<SharedEvent> events, int? personId, int assetId, DateTime asOf)
        {
            decimal contributed = 0, consumed = 0, paymentsFrom = 0, paymentsTo = 0;

            foreach (var e in events)
            {
                foreach (var m in (e.Movements ?? new List<SharedEventMovement>())
                    .Where(m => m.AssetId == assetId && m.Date.Date <= asOf.Date))
                {
                    if (m.PayerPersonId == personId) contributed += m.TotalAmount;
                    consumed += m.Shares?.Where(s => s.PersonId == personId).Sum(s => s.Amount) ?? 0;
                }

                foreach (var p in (e.Payments ?? new List<SharedEventPayment>())
                    .Where(p => p.AssetId == assetId && p.Date.Date <= asOf.Date))
                {
                    if (p.FromPersonId == personId) paymentsFrom += p.Amount;
                    if (p.ToPersonId == personId) paymentsTo += p.Amount;
                }
            }

            return Math.Round(contributed - consumed + paymentsFrom - paymentsTo, 2);
        }
    }
}
