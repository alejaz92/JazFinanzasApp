namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class InvestmentTransactionsResult
    {
        public DateTime Date { get; set; }
        public string Account { get; set; }
        public string MovementType { get; set; }
        public string CommerceType { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuotePrice { get; set; }
        public decimal Total { get; set; }
    }
}
