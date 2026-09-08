namespace JazFinanzasApp.API.Business.DTO.Dashboard
{
    // Sección 6, Flujo 1: "una barra con lo que se lleva gastado, contra lo gastado a la misma
    // altura del mes pasado, y la proyección de cierre al ritmo actual".
    public class DashboardThermometerDTO
    {
        public decimal MonthToDateAmount { get; set; }
        public decimal PreviousMonthSameDayAmount { get; set; }
        public decimal ProjectedMonthEndAmount { get; set; }
        public int DaysElapsed { get; set; }
        public int DaysInMonth { get; set; }
    }
}
