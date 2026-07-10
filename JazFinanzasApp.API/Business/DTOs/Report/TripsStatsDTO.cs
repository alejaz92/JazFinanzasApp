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
    }

    public class TripClassBreakdownDTO
    {
        public string TransactionClass { get; set; }
        public decimal Amount { get; set; }
    }
}
