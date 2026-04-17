namespace JazFinanzasApp.API.Business.DTO.InvestmentTransaction
{
    public class CurrencyExchangeListDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? ExpenseAsset { get; set; }
        public string? ExpenseAccount { get; set; }
        public string? ExpensePortfolio { get; set; }
        public decimal? ExpenseAmount { get; set; }
        public string? IncomeAsset { get; set; }
        public string? IncomeAccount { get; set; }
        public string? IncomePortfolio { get; set; }
        public decimal? IncomeAmount { get; set; }
    }
}
