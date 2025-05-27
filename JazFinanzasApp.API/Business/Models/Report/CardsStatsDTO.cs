using JazFinanzasApp.API.Business.DTO.CardTransaction;

namespace JazFinanzasApp.API.Business.DTO.Report
{
    public class CardsStatsDTO
    {
        public CardGraphDTO[] PesosCardGraphDTO { get; set; }
        public CardGraphDTO[] DollarsCardGraphDTO { get; set; }
        public CardTransactionPaymentListDTO[] cardTransactionsDTO { get; set; } 
    }

    public class CardGraphDTO
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }

    }


}
