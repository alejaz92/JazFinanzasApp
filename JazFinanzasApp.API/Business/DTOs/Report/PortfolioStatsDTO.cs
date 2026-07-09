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
}
