using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Business.DTO.SharedExpense
{
    public class SharedExpenseSplitDTO
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public string PersonName { get; set; }
        public SharedExpenseSplitType SplitType { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountReimbursed { get; set; }
        public decimal AmountApplied { get; set; }
        public decimal InstallmentSplitAmount { get; set; }
        public SharedExpenseSplitStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
