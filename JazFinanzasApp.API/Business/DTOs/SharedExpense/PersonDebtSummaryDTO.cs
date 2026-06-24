namespace JazFinanzasApp.API.Business.DTO.SharedExpense
{
    public class PersonDebtSummaryDTO
    {
        public int PersonId { get; set; }
        public string PersonName { get; set; }
        public decimal TotalPending { get; set; }
        public List<PersonDebtSplitDTO> Splits { get; set; } = new();
    }

    public class PersonDebtSplitDTO
    {
        public int SplitId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountReimbursed { get; set; }
        public decimal Pending { get; set; }
        public Domain.SharedExpenseSplitStatus Status { get; set; }
        public int? CardTransactionId { get; set; }
        public int? TransactionId { get; set; }
    }
}
