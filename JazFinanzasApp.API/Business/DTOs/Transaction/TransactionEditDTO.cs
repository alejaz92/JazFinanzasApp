namespace JazFinanzasApp.API.Business.DTO.Transaction
{
    public class TransactionEditDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int AssetId { get; set; }

        public int AccountID { get; set; }

        public int TransactionClassId { get; set; }

        public string Detail { get; set; }

        public decimal Amount { get; set; }
    }
}
