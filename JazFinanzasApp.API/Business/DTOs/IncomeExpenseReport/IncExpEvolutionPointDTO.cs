namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class IncExpEvolutionPointDTO
    {
        public DateTime Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Result { get; set; }
        public decimal CumulativeResult { get; set; }

        // D-A: promedio móvil de 6 meses de gasto. Null mientras no haya 6 meses previos —
        // se degrada igual que la comparación interanual (D-B), no se rellena con menos datos.
        public decimal? ExpenseMovingAverage { get; set; }
    }
}
