using JazFinanzasApp.API.Business.DTO.SharedEvent;

namespace JazFinanzasApp.API.Business.DTO.SharedEventReport
{
    // Flujo 7 — Compartidos (Fase 21, docs/plans/activos/plan-rediseno-reportes-v2.md). El saldo actual
    // (Balances) reusa íntegro SharedEventConsolidatedDebtDTO/ISharedEventService.GetConsolidatedDebtsAsync
    // — no se convierte a una sola moneda: cada saldo nace en su propia (mismo criterio ya usado por
    // DashboardService para "Saldo compartido" en Inicio). BalanceEvolution y EventRanking son nuevos y
    // se calculan solo sobre Eventos (movimientos y pagos, que tienen fecha) — el pool de SharedExpense
    // sueltas (fuera de un Evento) no entra: su AmountReimbursed no tiene fecha histórica reconstruible,
    // así que un "saldo a tal mes" con ese pool adentro no se puede recalcular hacia atrás.
    public class SharedEventGeneralReportDTO
    {
        public List<SharedEventConsolidatedDebtDTO> Balances { get; set; } = new();
        public List<SharedEventBalancePointDTO> BalanceEvolution { get; set; } = new();
        public List<SharedEventRankingDTO> EventRanking { get; set; } = new();
    }

    // Un punto por mes y por moneda -- "saldo total" no mezcla monedas. Es la parte propia
    // (Contributed - Consumed + pagos) agregada de todos los Eventos del usuario a esa fecha; no
    // incluye el pool de SharedExpense sueltas (ver nota de arriba), así que es un subconjunto de lo
    // que muestra "Saldo compartido" en Inicio, no el mismo número.
    public class SharedEventBalancePointDTO
    {
        public DateTime Month { get; set; }
        public int AssetId { get; set; }
        public string AssetSymbol { get; set; } = string.Empty;
        public decimal MyBalance { get; set; }
    }

    public class SharedEventRankingDTO
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public bool IsClosed { get; set; }
        public List<SharedEventAssetAmountDTO> Amounts { get; set; } = new();
    }

    public class SharedEventAssetAmountDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetSymbol { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class SharedEventPersonReportDTO
    {
        public int PersonId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public List<SharedEventConsolidatedDebtDTO> Balances { get; set; } = new();
        public List<SharedEventBalancePointDTO> BalanceEvolution { get; set; } = new();
        public List<SharedEventCategoryTotalDTO> CategoryTotals { get; set; } = new();
        public List<SharedEventPersonMovementDTO> Movements { get; set; } = new();
        public List<SharedEventPersonPaymentDTO> Payments { get; set; } = new();
    }

    // SharedEventMovementDTO/SharedEventPaymentDTO no llevan a qué evento pertenecen (tiene sentido
    // adentro de SharedEventDTO, donde el evento ya es el contexto) -- acá el historial cruza varios
    // eventos, así que se envuelven con el dato que les falta en vez de duplicar sus campos.
    public class SharedEventPersonMovementDTO
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public SharedEventMovementDTO Movement { get; set; } = null!;
    }

    public class SharedEventPersonPaymentDTO
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public SharedEventPaymentDTO Payment { get; set; } = null!;
    }
}
