namespace JazFinanzasApp.API.Business.DTO.SharedExpense
{
    public class SharedExpenseDetailDTO
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string? Notes { get; set; }
        public List<SharedExpenseSplitDTO> Splits { get; set; } = new();
    }
}
