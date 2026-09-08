namespace JazFinanzasApp.API.Business.DTO.Dashboard
{
    // Un neto por moneda (eventos + gastos sueltos V1, mismo cálculo que
    // ISharedEventService.GetConsolidatedDebtsAsync) — no se convierte a la moneda de referencia
    // elegida porque cada saldo compartido nace en una moneda propia y mezclarlas mentiría.
    public class DashboardSharedBalanceDTO
    {
        public int AssetId { get; set; }
        public string AssetSymbol { get; set; } = string.Empty;
        public decimal Net { get; set; }
    }

    public class DashboardIndicatorsDTO
    {
        public string ReferenceAssetSymbol { get; set; } = string.Empty;

        // Suma de los saldos de cuenta cuyo tipo de activo es "Moneda" (1.3 del plan: la separación
        // entre disponible e invertido sale del tipo de activo, no de una marca por cuenta) — mismo
        // dato que Patrimonio → Por cuenta, filtrado.
        public decimal Available { get; set; }

        // Mismos números que Patrimonio → General para este activo (NetWorthReportService.GetGeneralAsync).
        public decimal NetWorthGross { get; set; }
        public decimal NetWorthNet { get; set; }
        public decimal NetWorthChangeVsPreviousMonth { get; set; }

        // Mismos números que Ingresos y Egresos → Resumen del mes (IncExpWaterfallDTO).
        public decimal MonthResult { get; set; }
        public decimal MonthResultPreviousMonth { get; set; }

        // Siempre en pesos, no en ReferenceAssetSymbol — mismo criterio que
        // CardTransactionPaymentListDTO.ValueInPesos (Tarjetas → General, tabla de resumen del mes),
        // que este indicador resume sumando esa misma tabla.
        public decimal CardsDueAmountInPesos { get; set; }
        public DateTime? CardsNextDueDate { get; set; }

        public List<DashboardSharedBalanceDTO> SharedBalances { get; set; } = new();
    }
}
