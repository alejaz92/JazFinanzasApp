namespace JazFinanzasApp.API.Models.DTO.InvestmentTransaction
{
    public class StockTransactionAddDTO
    {
        public DateTime Date { get; set; }
        public string Environment { get; set; }
        public string AssetType { get; set; }
        public string StockMovementType { get; set; }
        public string CommerceType { get; set; }
        public int? ExpenseAssetId { get; set; }
        public int? ExpenseAccountId { get; set; }
        public int? ExpensePortafolioID { get; set; }
        public decimal? ExpenseQuantity { get; set; }
        public decimal? ExpenseQuotePrice { get; set; }
        public int? IncomeAssetId { get; set; }
        public int? IncomeAccountId { get; set; }
        public int? IncomePortafolioID { get; set; }
        public decimal? IncomeQuantity { get; set; }
        public decimal? IncomeQuotePrice { get; set; }
    }
}
