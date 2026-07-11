namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventActiveSummaryBalanceDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public decimal MyBalance { get; set; }
    }
}
