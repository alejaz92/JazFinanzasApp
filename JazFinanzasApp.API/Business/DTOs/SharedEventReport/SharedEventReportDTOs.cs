using JazFinanzasApp.API.Business.DTO.SharedEvent;

namespace JazFinanzasApp.API.Business.DTO.SharedEventReport
{
    // Flujo 7 — Compartidos (Fase 21, docs/plans/activos/plan-rediseno-reportes-v2.md). El saldo actual
    // (Balances) reusa íntegro SharedEventConsolidatedDebtDTO/ISharedEventService.GetConsolidatedDebtsAsync
    // — no se convierte a una sola moneda: cada saldo nace en su propia (mismo criterio ya usado por
    // DashboardService para "Saldo compartido" en Inicio). Hubo una evolución mensual del saldo
    // (BalanceEvolution) que se sacó tras la revisión de la Fase 22 — ver el comentario en
    // SharedEventReportService sobre por qué no se puede reconstruir de forma honesta.
    public class SharedEventGeneralReportDTO
    {
        public List<SharedEventConsolidatedDebtDTO> Balances { get; set; } = new();
        public List<SharedEventRankingDTO> EventRanking { get; set; } = new();
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
