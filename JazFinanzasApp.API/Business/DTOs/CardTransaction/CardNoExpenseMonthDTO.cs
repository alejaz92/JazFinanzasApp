namespace JazFinanzasApp.API.Business.DTO.CardTransaction
{
    public class CardNoExpenseMonthDTO
    {
        public int CardId { get; set; }
        public DateTime PaymentMonth { get; set; }
        public DateTime NextClosingDate { get; set; }
        public DateTime NextDueDate { get; set; }
    }
}
