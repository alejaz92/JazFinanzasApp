namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class IncomeCategorySeriesDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<decimal> MonthlyTrend { get; set; } = new();
    }
}
