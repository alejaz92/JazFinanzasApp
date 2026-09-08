namespace JazFinanzasApp.API.Business.DTO.CardReport
{
    public class CardPromotionsReportDTO
    {
        // Corrección 2026-09-05: los montos (TotalSaved con la cotización de hoy — es un agregado
        // histórico, mismo criterio que NetWorth para pasar de un total ya sumado a otra moneda —,
        // MonthlySeries con la cotización de cada mes, Pending con la de su propio CreditDate) vienen
        // convertidos a ReferenceAssetSymbol. Mismos 5 campos que CardGeneralReportDTO.
        public string ReferenceAssetSymbol { get; set; } = string.Empty;
        public string PesoAssetSymbol { get; set; } = string.Empty;
        public string PesoAssetColor { get; set; } = string.Empty;
        public string DollarAssetSymbol { get; set; } = string.Empty;
        public string DollarAssetColor { get; set; } = string.Empty;

        // Total histórico (no acotado a MonthlySeries): con 3 meses de historial real (1.4 del plan)
        // limitarlo a la ventana de 12 meses ocultaría reintegros reales.
        public decimal TotalSavedPesos { get; set; }
        public decimal TotalSavedDollars { get; set; }

        // Null cuando no hubo consumo en la moneda en los últimos 12 meses (no hay contra qué
        // porcentualizar). Pesos y dólares nunca se mezclan (mismo criterio que el resto de Tarjetas).
        public decimal? PercentOfConsumptionPesos { get; set; }
        public decimal? PercentOfConsumptionDollars { get; set; }

        public List<PromotionMonthDTO> MonthlySeries { get; set; } = new();
        public List<PendingReimbursementDTO> Pending { get; set; } = new();
    }

    public class PromotionMonthDTO
    {
        public DateTime Month { get; set; }
        public decimal PesosAmount { get; set; }
        public decimal DollarsAmount { get; set; }
    }

    // AmountApplied < Amount: todavía queda algo por acreditar en una cuenta, por aplicar a una
    // cuota, o ambas.
    public class PendingReimbursementDTO
    {
        public int DiscountId { get; set; }
        public int CardTransactionId { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;

        // Moneda nativa del reintegro (la del CardTransaction que lo generó) — la usa el servicio
        // para elegir la cotización correcta al convertir a ReferenceAssetSymbol.
        public string AssetName { get; set; } = string.Empty;

        public decimal PendingToCredit { get; set; }
        public decimal PendingToApply { get; set; }
        public DateTime CreditDate { get; set; }
    }
}
