namespace JazFinanzasApp.API.Business.DTO.CardReport
{
    public class CardPromotionsReportDTO
    {
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
        public decimal PendingToCredit { get; set; }
        public decimal PendingToApply { get; set; }
        public DateTime CreditDate { get; set; }
    }
}
