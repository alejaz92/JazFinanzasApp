namespace JazFinanzasApp.API.Business.DTO.Report
{
    public class PortfolioStatsDTO
    {
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; }
        public bool IsDefault { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
    }

    public class PortfolioDetailStatsDTO
    {
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        public PortfolioHoldingDTO[] Holdings { get; set; }
    }

    public class PortfolioHoldingDTO
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
