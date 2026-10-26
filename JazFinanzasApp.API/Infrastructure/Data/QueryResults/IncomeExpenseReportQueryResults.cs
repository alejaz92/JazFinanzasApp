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

    // Composición de ingresos de UN mes puntual (sin rubro, sin tendencia) — corrección 2026-09-04:
    // el usuario quería poder elegir un mes y ver su composición, no una evolución en el tiempo.
    public class IncomeCategoryAmountResult
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    // Una fila por (categoría, día) con ingreso real — dato crudo sin agregar, para que el
    // frontend arme las 3 variantes de "días de cobro" en comparación (tabla/timeline/calendario
    // por categoría) sin pedir un endpoint distinto por cada una.
    // Gasto de una categoría en el tramo "del 1 al día N" del mes pedido, contra el mismo tramo del
    // mes anterior y el promedio del mismo tramo en los meses anteriores (plan-devoluciones-llm.md, D2).
    // Los meses sin gasto cuentan como cero en el promedio.
    public class CategoryToDateSpendingResult
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public decimal CurrentToDate { get; set; }
        public decimal PreviousToDate { get; set; }
        public decimal AverageToDate { get; set; }
    }

    // Un egreso individual, con su monto expresado en cada una de las monedas pedidas.
    public class ExpenseMovementResult
    {
        public int TransactionId { get; set; }
        public DateTime Date { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? Detail { get; set; }

        // Monto positivo, por assetId de la moneda pedida.
        public Dictionary<int, decimal> AmountByAssetId { get; set; } = new();
    }

    // Un movimiento de ingreso o egreso con su monto ya expresado en pesos, para las alertas por reglas
    // (plan-devoluciones-llm.md, D8). Monto positivo; MovementType es "I" o "E".
    public class AlertMovementResult
    {
        public DateTime Date { get; set; }
        public string MovementType { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? Detail { get; set; }
        public decimal AmountInPesos { get; set; }
    }

    // Los tres orígenes del gasto por fecha de compra (plan-devoluciones-llm-v2.md, D1).
    public static class PurchaseSpendingKind
    {
        // Un egreso pagado directo desde una cuenta, en su fecha.
        public const string Direct = "Direct";
        // Una compra con tarjeta (de una vez o en cuotas), entera, en su fecha de compra.
        public const string CardPurchase = "CardPurchase";
        // Un cargo recurrente de tarjeta, su monto en cada mes en que estuvo activo, el día 1 del mes.
        public const string CardRecurring = "CardRecurring";
    }

    // Una fila del gasto por fecha de compra, con su monto expresado en cada moneda pedida (positivo, sin redondear).
    public class PurchaseSpendingRowResult
    {
        public DateTime Date { get; set; }
        public string Kind { get; set; } = PurchaseSpendingKind.Direct;
        // Id del movimiento (Direct) o del consumo de tarjeta (CardPurchase y CardRecurring).
        public int SourceId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? Detail { get; set; }
        public Dictionary<int, decimal> AmountByAssetId { get; set; } = new();
    }

    // La cuota de un consumo de tarjeta que cae en un mes (la segunda lectura, plan-devoluciones-llm-v2.md, D1).
    public class CardInstallmentDueResult
    {
        public int CardTransactionId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? Detail { get; set; }
        public bool IsRecurring { get; set; }
        public decimal Amount { get; set; }
    }

    public class IncomeCategoryDayResult
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
