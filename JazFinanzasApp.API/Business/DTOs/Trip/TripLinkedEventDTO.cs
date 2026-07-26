namespace JazFinanzasApp.API.Business.DTO.Trip
{
    public class TripLinkedEventDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsClosed { get; set; }
        public int ParticipantCount { get; set; }
        public int MovementCount { get; set; }
        public List<TripLinkedEventTotalDTO> Totals { get; set; } = new();
    }

    public class TripLinkedEventTotalDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public decimal Amount { get; set; }
    }
}
