namespace JazFinanzasApp.API.Business.DTO.AssetSplitEvent
{
    public class AssetSplitEventListDTO
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public DateTime Date { get; set; }
        public decimal SplitRatio { get; set; }
    }
}
