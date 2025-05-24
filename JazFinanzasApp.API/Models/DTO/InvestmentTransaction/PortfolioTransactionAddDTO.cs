namespace JazFinanzasApp.API.Models.DTO.InvestmentTransaction
{
    public class PortfolioTransactionAddDTO
    {
        public DateTime Date { get; set; }
        public int AssetId { get; set; }
        public int AccountId { get; set; }        
        public decimal Amount { get; set; }
        public int? ExpensePortfolioID { get; set; }
        public int? IncomePortfolioID { get; set; }

    }
}

