namespace JazFinanzasApp.API.Models.DTO.Transaction
{
    public class TransactionAddDTO
    {
        public int? incomeAccountId { get; set; }
        public int? expenseAccountId { get; set; }

        public int? incomePortfolioId { get; set; }
        public int? expensePortfolioId { get; set; }
        public int assetId { get; set; }
        public DateTime date { get; set; }
        public string movementType { get; set; }
        public int? transactionClassId { get; set; }
        public string detail { get; set; }
        public decimal amount { get; set; }
        public decimal quotePrice { get; set; }
    }
}
