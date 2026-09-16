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
    // repartió, qué quedó pendiente" -- este servicio cubre General (Balances actuales + ranking) y
    // Por persona (lo mismo, más el historial cruzando eventos).
    //
    // Hubo una evolución mensual del saldo (BalanceEvolution), sacada tras la revisión de la Fase 22:
    // solo podía reconstruirse a partir de movimientos/pagos de Evento, que tienen fecha. El pool de
    // SharedExpense sueltas (fuera de un Evento) --que es de donde sale la mayoría del saldo real,
    // Renzo en `demo` incluido-- no tiene esa fecha en el 89% de los casos históricos (solo el total
    // acumulado de hoy), así que el gráfico terminaba mostrando una "evolución" que en la práctica
    // podía no moverse nunca mientras el saldo real de la persona sí lo hacía -- contradecía a la
    // tabla de "Saldo actual" de la misma pantalla sin que hubiera forma honesta de conciliarlo mes a
    // mes. Se prefirió sacarlo a dejar un gráfico que mintiera por construcción.
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
                CategoryTotals = categoryTotals,
                Movements = movements,
                Payments = payments
            };
        }
    }
}
