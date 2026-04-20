namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class TotalBalanceResult
    {
        public decimal? Total { get; set; }
    }

    public class BalanceResult
    {
        public string Account { get; set; }
        public decimal Balance { get; set; }
    }

    public class TotalsBalanceResult
    {
        public string Asset { get; set; }
        public string Symbol { get; set; }
        public string Color { get; set; }
        public decimal Balance { get; set; }
    }
}
