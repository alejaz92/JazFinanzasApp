
namespace JazFinanzasApp.API.Models.DTO.CardTransaction
{
    public class CardTransactionsPendingDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Card  { get; set; }
        public string TransactionClass { get; set; }          
        public string Detail { get; set; }
        public string Installments { get; set; }
        public string Asset { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime FirstInstallment { get; set; }
        public string LastInstallment { get; set; }
        public decimal InstallmentAmount { get; set; }

    }
}
