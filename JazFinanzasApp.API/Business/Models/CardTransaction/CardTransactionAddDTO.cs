namespace JazFinanzasApp.API.Business.DTO.CardTransaction
{
    public class CardTransactionAddDTO
    {
        public DateTime Date { get; set; }
        public string Detail { get; set; }
        public int CardId { get; set; }
        public int TransactionClassId { get; set; }
        public int AssetId { get; set; }
        public decimal TotalAmount { get; set; }
        public int Installments { get; set; }    
        public DateTime FirstInstallment { get; set; }
        public DateTime LastInstallment { get; set; }
        public string Repeat { get; set; }
    }
}
