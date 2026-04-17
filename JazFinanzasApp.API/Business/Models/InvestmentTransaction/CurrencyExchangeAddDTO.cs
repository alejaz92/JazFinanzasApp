namespace JazFinanzasApp.API.Business.DTO.InvestmentTransaction
{
    public class CurrencyExchangeAddDTO
    {
        public DateTime Date { get; set; }
        public int? ExpenseAssetId { get; set; }
        public int? ExpenseAccountId { get; set; }
        public int? ExpensePortfolioID { get; set; }
        public decimal? ExpenseAmount { get; set; }
        public int? IncomeAssetId { get; set; }
        public int? IncomeAccountId { get; set; }
        public int? IncomePortfolioID { get; set; }
        public decimal? IncomeAmount { get; set; }
    }
}
