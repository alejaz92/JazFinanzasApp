namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class StockStatsListResult
    {
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
    }

    public class StocksGralStatsResult
    {
        public string AssetType { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
    }
}
