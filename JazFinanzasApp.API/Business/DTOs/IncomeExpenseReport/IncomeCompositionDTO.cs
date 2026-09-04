namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class IncomeCategoryAmountDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    public class IncomeCompositionDTO
    {
        public DateTime Month { get; set; }
        public List<IncomeCategoryAmountDTO> Categories { get; set; } = new();
    }
}
