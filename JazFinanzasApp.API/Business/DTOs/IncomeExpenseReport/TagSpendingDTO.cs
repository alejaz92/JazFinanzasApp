namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class MonthlyAmountDTO
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class CategoryAmountDTO
    {
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    public class TagSpendingDTO
    {
        public int TagId { get; set; }
        public string TagName { get; set; }
        public string? Color { get; set; }
        public decimal TotalAmount { get; set; }
        public List<MonthlyAmountDTO> MonthlyEvolution { get; set; } = new();
        public List<CategoryAmountDTO> ByCategory { get; set; } = new();
    }
}
