namespace JazFinanzasApp.API.Business.DTO.CardReport
{
    public class CardFutureCommitmentDTO
    {
        // Corrección 2026-09-05: Amount/InstallmentAmount vienen convertidos a ReferenceAssetSymbol,
        // con la cotización de hoy (una fecha futura no tiene cotización propia — cae en la más
        // reciente disponible, que termina siendo la de hoy). Mismos 5 campos que CardGeneralReportDTO.
        public string ReferenceAssetSymbol { get; set; } = string.Empty;
        public string PesoAssetSymbol { get; set; } = string.Empty;
        public string PesoAssetColor { get; set; } = string.Empty;
        public string DollarAssetSymbol { get; set; } = string.Empty;
        public string DollarAssetColor { get; set; } = string.Empty;

        public List<FutureCommitmentMonthDTO> MonthlySeries { get; set; } = new();
        public List<FutureCommitmentPurchaseDTO> Timeline { get; set; } = new();
    }

    public class FutureCommitmentMonthDTO
    {
        public DateTime Month { get; set; }
        public List<FutureCommitmentPurchaseAmountDTO> Purchases { get; set; } = new();
    }

    // Una entrada por compra en cuotas todavía viva ese mes. Corrección 2026-09-05, cuarta ronda:
    // el color de la columna apilada pasó de ser "una compra" a ser "una categoría" (más legible con
    // varias compras vivas a la vez) — TransactionClassId/Name son lo que el frontend agrupa; sigue
    // viajando CardTransactionId/Detail para el drill-down por panel lateral al hacer clic.
    public class FutureCommitmentPurchaseAmountDTO
    {
        public int CardTransactionId { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public int TransactionClassId { get; set; }
        public string TransactionClassName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    // Una fila por compra en cuotas viva, para el cronograma (barra que arranca y termina).
    public class FutureCommitmentPurchaseDTO
    {
        public int CardTransactionId { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public decimal InstallmentAmount { get; set; }
        public DateTime StartMonth { get; set; }
        public DateTime EndMonth { get; set; }
        public int RemainingInstallments { get; set; }
    }
}
