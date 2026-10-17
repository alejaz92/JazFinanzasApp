namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    // Gasto por categoría "a la misma altura del mes" (plan-devoluciones-llm.md, D2): del día 1 al día
    // CutoffDay del mes pedido, contra el mismo tramo del mes anterior y el promedio del mismo tramo en
    // los ComparisonMonths meses anteriores (los meses sin gasto cuentan como cero). Con CutoffDay igual
    // a la cantidad de días del mes, es el mes completo.
    public class SpendingToDateCategoryDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ParentName { get; set; }
        public decimal CurrentToDate { get; set; }
        public decimal PreviousToDate { get; set; }
        public decimal AverageToDate { get; set; }
    }

    public class SpendingToDateDTO
    {
        public DateTime Month { get; set; }
        public int CutoffDay { get; set; }
        public int ComparisonMonths { get; set; }
        public decimal TotalCurrent { get; set; }
        public decimal TotalPrevious { get; set; }
        public decimal TotalAverage { get; set; }
        public List<SpendingToDateCategoryDTO> Categories { get; set; } = new();
    }
}
