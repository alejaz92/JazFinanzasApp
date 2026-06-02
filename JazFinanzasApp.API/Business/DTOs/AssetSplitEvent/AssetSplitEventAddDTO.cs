namespace JazFinanzasApp.API.Business.DTO.AssetSplitEvent
{
    public class AssetSplitEventAddDTO
    {
        public int AssetId { get; set; }
        public DateTime Date { get; set; }
        public decimal SplitRatio { get; set; }
    }
}
