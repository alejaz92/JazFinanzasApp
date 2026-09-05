using JazFinanzasApp.API.Business.DTO.CardTransaction;

namespace JazFinanzasApp.API.Business.DTO.CardReport
{
    public class CardGeneralReportDTO
    {
        public List<CardMonthlySeriesPointDTO> MonthlySeries { get; set; } = new();
        public List<CardTransactionPaymentListDTO> CurrentMonthSummary { get; set; } = new();
    }

    public class CardMonthlySeriesPointDTO
    {
        public DateTime Month { get; set; }
        public List<CardMonthAmountDTO> Cards { get; set; } = new();
    }

    public class CardMonthAmountDTO
    {
        public int CardId { get; set; }
        public string CardName { get; set; } = string.Empty;
        public decimal PesosAmount { get; set; }
        public decimal DollarsAmount { get; set; }
    }
}
