using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;

namespace JazFinanzasApp.API.Business.DTO.PurchaseSpending
{
    // La segunda lectura de la tarjeta (plan-devoluciones-llm-v2.md, D1): lo que toca pagar en cuotas en un mes, que no es lo
    // mismo que lo comprado en el mes. Incluye las cuotas de compras anteriores que caen ese mes y los cargos recurrentes.
    public class InstallmentsDueCategoryDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    // Todo el gasto por fecha de compra que necesita una devolución de un mes, de una sola consulta a la base
    // (plan-devoluciones-llm-v2.md, Fase 3): el tramo "del 1 al día de corte" con su comparación, los meses completos
    // (para el total del mes anterior y el promedio) y la serie diaria de este mes y el anterior (para la proyección).
    public class PurchaseSpendingWindowDTO
    {
        public SpendingToDateDTO ToDate { get; set; } = new();

        // Con el día de corte en 31: cada mes completo. Su Previous y su Average son los totales de mes completo, que es
        // contra lo que se compara una proyección de cierre.
        public SpendingToDateDTO FullMonths { get; set; } = new();

        public List<DaySpendingDTO> Days { get; set; } = new();
    }

    public class InstallmentsDueDTO
    {
        public DateTime Month { get; set; }

        // Cuotas de compras en cuotas (de una vez o en cuotas fijas) que caen en el mes.
        public decimal TotalInstallments { get; set; }

        // Cargos recurrentes de tarjeta activos en el mes (suscripciones, servicios).
        public decimal TotalRecurring { get; set; }

        public decimal Total { get; set; }

        // Por categoría, de mayor a menor.
        public List<InstallmentsDueCategoryDTO> Categories { get; set; } = new();
    }
}
