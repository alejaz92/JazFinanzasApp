namespace JazFinanzasApp.API.Business.DTO.CardReport
{
    public class CardDetailReportDTO
    {
        public int CardId { get; set; }
        public string CardName { get; set; } = string.Empty;
        public DateTime? NextClosingDate { get; set; }
        public DateTime? NextDueDate { get; set; }
        public decimal CurrentMonthPesos { get; set; }
        public decimal CurrentMonthDollars { get; set; }
        public List<CardCategoryAmountDTO> ByCategory { get; set; } = new();
        public List<CardSimpleMonthlyPointDTO> MonthlyEvolution { get; set; } = new();
    }

    public class CardCategoryAmountDTO
    {
        public int TransactionClassId { get; set; }
        public string TransactionClassName { get; set; } = string.Empty;
        public decimal PesosAmount { get; set; }
        public decimal DollarsAmount { get; set; }
    }

    public class CardSimpleMonthlyPointDTO
    {
        public DateTime Month { get; set; }
        public decimal PesosAmount { get; set; }
        public decimal DollarsAmount { get; set; }
    }
}
