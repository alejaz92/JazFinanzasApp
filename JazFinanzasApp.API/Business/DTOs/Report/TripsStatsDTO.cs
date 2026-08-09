namespace JazFinanzasApp.API.Business.DTO.Report
{
    public class TripsGeneralStatsDTO
    {
        public int TripId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public decimal TotalInReference { get; set; }
    }

    public class TripDetailStatsDTO
    {
        public int TripId { get; set; }
        public string Name { get; set; }
        public decimal Total { get; set; }
        public TripClassBreakdownDTO[] Breakdown { get; set; }
        public TripEventNetDTO[] NetBreakdown { get; set; }
    }

    public class TripClassBreakdownDTO
    {
        public string TransactionClass { get; set; }
        public decimal Amount { get; set; }
    }

    public class TripEventNetDTO
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public decimal Amount { get; set; }
    }

    // Los dos totales de trip-detail (docs/plans/activos/plan-detalle-viaje-montos-propios.md, Fase 2), en la
    // moneda de referencia principal del usuario.
    public class TripTotalsDTO
    {
        public decimal OwnTotal { get; set; }
        public decimal GrossTotal { get; set; }
    }
}
