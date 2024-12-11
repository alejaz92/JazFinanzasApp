namespace JazFinanzasApp.API.Models.DTO.CardTransaction
{
    public class CardTransactionPaymentListDTO
    {
        public DateTime Date { get; set; }
        public int CardId { get; set; } 
        public string? Card { get; set; }
        public int TransactionClassId { get; set; }
        public string? TransactionClass { get; set; }   
        public string Detail { get; set; }
        public int AssetId { get; set; }
        public string? Asset { get; set; }
        public string Installment { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal ValueInPesos { get; set; }
    }
}
