namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class PortfolioStatsResult
    {
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; }
        public bool IsDefault { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
    }

    public class PortfolioHoldingResult
    {
        public string AssetType { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public string AccountName { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
    }
}
