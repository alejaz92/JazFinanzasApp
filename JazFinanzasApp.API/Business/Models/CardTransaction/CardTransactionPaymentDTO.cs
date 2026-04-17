namespace JazFinanzasApp.API.Business.DTO.CardTransaction
{
    public class CardTransactionPaymentDTO
    {
        public int CardId { get; set; }
        public DateTime PaymentMonth { get; set; }
        public DateTime PaymentDate { get; set; }
        public int accountId { get; set; }
        public string PaymentAsset { get; set; }
        public decimal PesosAmount { get; set; }
        public decimal? DolarAmount { get; set; }
        public decimal CardExpenses { get; set; }

        public List<CardTransactionPaymentListDTO> CardTransactions { get; set; }
     
    }
}
