namespace JazFinanzasApp.API.Business.DTO.TripReport
{
    // Flujo 6 — Viajes (Fase 21, docs/plans/activos/plan-rediseno-reportes-v2.md). Mismo Total que
    // ReportService.GetTripsGeneralStatsAsync (dos fuentes disjuntas: neto de Eventos vinculados +
    // gastos propios etiquetados con TripId), con assetId explícito en vez de la moneda principal
    // resuelta del usuario (T12) y los dos datos nuevos del Flujo 6: duración y costo por día.
    public class TripGeneralReportDTO
    {
        public int TripId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public decimal Total { get; set; }
        public decimal CostPerDay { get; set; }
    }

    public class TripDetailReportDTO
    {
        public int TripId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public decimal Total { get; set; }
        public decimal CostPerDay { get; set; }
        public TripReportClassBreakdownDTO[] Breakdown { get; set; } = Array.Empty<TripReportClassBreakdownDTO>();
        public TripReportEventNetDTO[] NetBreakdown { get; set; } = Array.Empty<TripReportEventNetDTO>();
        public TripDailySpendingDTO[] DailySpending { get; set; } = Array.Empty<TripDailySpendingDTO>();

        // Total - Sum(DailySpending): un movimiento (o gasto propio) etiquetado al viaje pero con fecha
        // fuera de [StartDate, EndDate] -- pasajes comprados con meses de anticipación, ropa comprada
        // antes, etc. (visto en producción con el viaje "Mar Chiquita 2026" de demo: dos de sus tres
        // movimientos tienen fecha de 2024 y de dos meses antes del viaje). Sin este campo esa plata
        // desaparecía en silencio del gráfico de día a día aunque siguiera sumada en Total.
        public decimal OutsideTripRangeAmount { get; set; }

        public TripCostPerDayComparisonDTO[] CostPerDayComparison { get; set; } = Array.Empty<TripCostPerDayComparisonDTO>();
    }

    public class TripReportClassBreakdownDTO
    {
        public string TransactionClass { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class TripReportEventNetDTO
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    // Un punto por día del viaje (StartDate a EndDate inclusive), en 0 los días sin movimientos —
    // para que el gráfico de gasto día a día no tenga huecos.
    public class TripDailySpendingDTO
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    // Costo por día de cada viaje del usuario, para comparar el actual contra los demás (Flujo 6,
    // Detalle) sin repetir el cálculo de GetGeneralAsync desde el frontend.
    public class TripCostPerDayComparisonDTO
    {
        public int TripId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal CostPerDay { get; set; }
        public bool IsCurrent { get; set; }
    }
}
