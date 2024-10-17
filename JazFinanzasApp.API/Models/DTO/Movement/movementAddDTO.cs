namespace JazFinanzasApp.API.Models.DTO.Movement
{
    public class movementAddDTO
    {
        public int? incomeAccountId { get; set; }
        public int? expenseAccountId { get; set; }
        public int assetId { get; set; }
        public DateTime date { get; set; }
        public string movementType { get; set; }
        public int? movementClassId { get; set; }
        public string detail { get; set; }
        public decimal amount { get; set; }
        public decimal quotePrice { get; set; }
    }
}
