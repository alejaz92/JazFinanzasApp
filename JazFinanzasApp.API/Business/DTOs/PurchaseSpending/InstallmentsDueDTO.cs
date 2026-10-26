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
