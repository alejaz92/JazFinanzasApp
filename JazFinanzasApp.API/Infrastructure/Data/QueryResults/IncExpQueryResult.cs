namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class IncExpResult
    {
        public ClassIncomeResult[] ClassIncomeStats { get; set; }
        public ClassExpenseResult[] ClassExpenseStats { get; set; }
        public MonthIncomeResult[] MonthIncomeStats { get; set; }
        public MonthExpenseResult[] MonthExpenseStats { get; set; }
    }

    public class ClassIncomeResult
    {
        public string TransactionClass { get; set; }
        public decimal Amount { get; set; }
    }

    public class ClassExpenseResult
    {
        public string TransactionClass { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthIncomeResult
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthExpenseResult
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
    }
}
