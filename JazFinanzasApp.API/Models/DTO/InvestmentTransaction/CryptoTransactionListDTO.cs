namespace JazFinanzasApp.API.Models.DTO.InvestmentTransaction
{
    public class CryptoTransactionListDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string MovementType { get; set; }
        public string CommerceType { get; set; }
        public string? ExpenseAsset { get; set; }
        public string? ExpenseAccount { get; set; }
        public string? ExpensePortafolio { get; set; }
        public decimal? ExpenseAmount { get; set; }
        public decimal? ExpenseQuote { get; set; }
        public string? IncomeAsset { get; set; }
        public string? IncomeAccount { get; set; }
        public string? IncomePortafolio { get; set; }
        public decimal? IncomeAmount { get; set; }
        public decimal? IncomeQuote { get; set; }

    }
}
