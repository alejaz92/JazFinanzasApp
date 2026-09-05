namespace JazFinanzasApp.API.Business.DTO.CardReport
{
    public class CardFutureCommitmentDTO
    {
        public List<FutureCommitmentMonthDTO> MonthlySeries { get; set; } = new();
        public List<FutureCommitmentPurchaseDTO> Timeline { get; set; } = new();
    }

    public class FutureCommitmentMonthDTO
    {
        public DateTime Month { get; set; }
        public List<FutureCommitmentPurchaseAmountDTO> Purchases { get; set; } = new();
    }

    // Una entrada por compra en cuotas todavía viva ese mes — cada una es "un color" en la columna
    // apilada (sección 6, Flujo 4).
    public class FutureCommitmentPurchaseAmountDTO
    {
        public int CardTransactionId { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
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
