namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class CardGraphResult
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class CardTransactionPendingResult
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Card { get; set; }
        public string TransactionClass { get; set; }
        public string Detail { get; set; }
        public string Installments { get; set; }
        public string Asset { get; set; }
        public string AssetSymbol { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime FirstInstallment { get; set; }
        public string LastInstallment { get; set; }
        public decimal InstallmentAmount { get; set; }
        public int? TripId { get; set; }
        public string? TripName { get; set; }
    }
}
