namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class WaterfallStepResult
    {
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    public class IncExpWaterfallResult
    {
        public DateTime Month { get; set; }
        public decimal TotalIncome { get; set; }
        public List<WaterfallStepResult> ExpenseSteps { get; set; } = new();
        public decimal TotalExpense { get; set; }
        public decimal Result { get; set; }
        public decimal PreviousMonthResult { get; set; }
    }

    public class IncExpEvolutionPointResult
    {
        public DateTime Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Result { get; set; }
    }

    // Una fila por categoría con gasto en la ventana pedida (T4: a lo sumo un nivel de rubro).
    public class CategorySpendingResult
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }

        // Ascendente, el último elemento es el mes pedido.
        public List<decimal> MonthlyTrend { get; set; } = new();
    }

    public class MonthlyAmountResult
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class CategoryAmountResult
    {
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    public class TagSpendingResult
    {
        public int TagId { get; set; }
        public string TagName { get; set; }
        public string? Color { get; set; }
        public decimal TotalAmount { get; set; }
        public List<MonthlyAmountResult> MonthlyEvolution { get; set; } = new();
        public List<CategoryAmountResult> ByCategory { get; set; } = new();
    }

    public class DailySpendingResult
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    // Sueldo/Aporte familiar/etc. — sin rubro (no aplica a ingresos), a diferencia de
    // CategorySpendingResult que sí lo tiene para egresos.
    public class IncomeCategorySeriesResult
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        // Ascendente, el último elemento es el mes en curso.
        public List<decimal> MonthlyTrend { get; set; } = new();
    }

    // Un día del mes (1-31) — cuánto se suele cobrar ese día y con qué frecuencia, sobre la
    // ventana de meses pedida. MonthsInWindow varía por día (el 31 no existe en todos los meses).
    public class PayDayResult
    {
        public int Day { get; set; }
        public decimal AverageAmountWhenReceived { get; set; }
        public int TimesReceived { get; set; }
        public int MonthsInWindow { get; set; }
    }
}
