namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class DaySpendingDTO
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    public class WeekdayAverageDTO
    {
        public DayOfWeek DayOfWeek { get; set; }
        public decimal Average { get; set; }
    }

    public class SpendingCalendarDTO
    {
        public int Year { get; set; }
        public List<DaySpendingDTO> Days { get; set; } = new();
        public List<WeekdayAverageDTO> WeekdayAverages { get; set; } = new();
    }
}
