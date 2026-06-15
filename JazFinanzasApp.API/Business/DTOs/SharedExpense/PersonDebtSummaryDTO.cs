namespace JazFinanzasApp.API.Business.DTO.SharedExpense
{
    public class PersonDebtSummaryDTO
    {
        public int PersonId { get; set; }
        public string PersonName { get; set; }
        public decimal TotalPending { get; set; }
    }
}
