namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class PayDayDTO
    {
        public int Day { get; set; }
        public decimal AverageAmountWhenReceived { get; set; }
        public int TimesReceived { get; set; }
        public int MonthsInWindow { get; set; }

        // Cuántos de los meses de la ventana tuvieron ingreso este día — la fecha confiable no es
        // la de mayor promedio sino la de mayor frecuencia (un ingreso ocasional grande no es un
        // día de cobro habitual).
        public decimal FrequencyPct => MonthsInWindow > 0 ? Math.Round((decimal)TimesReceived / MonthsInWindow * 100, 1) : 0;
    }

    public class PayDayCalendarDTO
    {
        public List<PayDayDTO> Days { get; set; } = new();
    }
}
