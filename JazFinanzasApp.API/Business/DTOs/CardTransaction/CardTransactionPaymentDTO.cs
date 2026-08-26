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

        // Cuanto saldo a favor de la tarjeta aplico el banco en este resumen. Lo confirma el usuario
        // contra el resumen: no se deduce, porque CardExpenses ya es una resta comodin y
        // despejarlo de ahi escondería cualquier error. Ver plan-reintegro-saldo-tarjeta.md (D6).
        public decimal CardCreditApplied { get; set; }
        public DateTime? NextClosingDate { get; set; }
        public DateTime? NextDueDate { get; set; }

        public List<CardTransactionPaymentListDTO> CardTransactions { get; set; }
     
    }
}
