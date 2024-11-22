namespace JazFinanzasApp.API.Models.DTO.InvestmentMovement
{
    public class StockTransactionListDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string AssetType { get; set; }
        public string StockTransactionType { get; set; }
        public string CommerceType { get; set; }
        public string? ExpenseAsset { get; set; }
        public string? ExpenseAccount { get; set; }
        public decimal? ExpenseQuantity { get; set; }
        public decimal? ExpenseQuotePrice { get; set; }
        public string? IncomeAsset { get; set; }
        public string? IncomeAccount { get; set; }
        public decimal? IncomeQuantity { get; set; }
        public decimal? IncomeQuotePrice { get; set; }
    }
}
