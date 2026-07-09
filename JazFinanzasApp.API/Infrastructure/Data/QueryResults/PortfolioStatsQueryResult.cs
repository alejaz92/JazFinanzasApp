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
}
