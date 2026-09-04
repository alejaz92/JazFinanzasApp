namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class IncomeCategoryDayDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
