namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class WaterfallStepDTO
    {
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    public class IncExpWaterfallDTO
    {
        public DateTime Month { get; set; }
        public decimal TotalIncome { get; set; }
        public List<WaterfallStepDTO> ExpenseSteps { get; set; } = new();
        public decimal TotalExpense { get; set; }
        public decimal Result { get; set; }

        // Comparación siempre presente (sección 7): el resultado del mes anterior, sin abrir sus pasos.
        public decimal PreviousMonthResult { get; set; }
    }
}
