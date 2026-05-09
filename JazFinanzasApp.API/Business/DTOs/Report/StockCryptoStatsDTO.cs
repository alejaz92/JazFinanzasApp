namespace JazFinanzasApp.API.Business.DTO.Report
{
    public class StockStatsDTO
    {
        public StockStatsListDTO[] StockStatsInd { get; set; }
        public StocksGralStatsDTO[] StockStatsGral { get; set; }
    }

    public class StockStatsListDTO
    {
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
    }

    public class StocksGralStatsDTO {         
        public string AssetType { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
    }
}
