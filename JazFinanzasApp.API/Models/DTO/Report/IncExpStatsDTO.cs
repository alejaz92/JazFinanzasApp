namespace JazFinanzasApp.API.Models.DTO.Report
{
    public class IncExpStatsDTO
    {
        public ClassIncomeStats[] ClassIncomeStats { get; set; }
        public ClassExpenseStats[] ClassExpenseStats { get; set; }
        public MonthIncomeStats[] MonthIncomeStats { get; set; }
        public MonthExpenseStats[] MonthExpenseStats { get; set; }
    }

    public class ClassIncomeStats
    {
        public string TransactionClass { get; set; }
        public decimal Amount { get; set; }
    }

    public class ClassExpenseStats
    {
        public string TransactionClass { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthIncomeStats
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthExpenseStats
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
    }
}
