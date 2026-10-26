using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.DTO.PurchaseSpending;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;

namespace JazFinanzasApp.API.Business.Interfaces
{
    // El gasto medido por fecha de compra (plan-devoluciones-llm-v2.md, D1): lo que realmente se gastó en un mes, sin importar
    // cuándo se cargó el pago del resumen de la tarjeta. Suma los egresos directos de cuenta, las compras con tarjeta en su fecha
    // y los recurrentes de tarjeta en cada mes activo; las cuotas pagadas no suman (ya contaron en la compra).
    //
    // No reemplaza a los reportes de Ingresos y Egresos: ese sigue respondiendo cuánto salió de las cuentas. Lo usan el asistente,
    // el termómetro de Panorama, el "Resultado del mes" de Inicio y las alertas por reglas.
    public interface IPurchaseSpendingService
    {
        // Gasto por categoría del tramo "del 1 al día N" del mes, contra el mismo tramo del mes anterior y el promedio del mismo
        // tramo en los `comparisonMonths` meses anteriores (los meses sin gasto cuentan como cero). Con `cutoffDay` igual a los días
        // del mes, es el mes completo. Mismo contrato que IIncomeExpenseReportService.GetSpendingToDateByCategoryAsync.
        Task<SpendingToDateDTO> GetToDateByCategoryAsync(int userId, int assetId, DateTime month, int cutoffDay, int comparisonMonths = 6);

        // Un monto por día en [from, toExclusive): la serie diaria del termómetro. Los recurrentes caen el día 1 de cada mes.
        Task<List<DaySpendingDTO>> GetDailySeriesAsync(int userId, int assetId, DateTime from, DateTime toExclusive);

        // Los egresos individuales más grandes de [from, toExclusive) (directos y compras con tarjeta; los recurrentes no son un
        // movimiento puntual), rankeados por la primera moneda y con su monto en todas, opcionalmente de una sola categoría.
        Task<List<PurchaseSpendingRowResult>> GetLargestAsync(int userId, IReadOnlyList<int> assetIds, DateTime from, DateTime toExclusive, int take, int? categoryId = null);

        // La segunda lectura: lo que toca pagar en cuotas en el mes (no es lo comprado en el mes).
        Task<InstallmentsDueDTO> GetInstallmentsDueAsync(int userId, int assetId, DateTime month);
    }
}
