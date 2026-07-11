namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventMovementDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public int TransactionClassId { get; set; }
        public string TransactionClassName { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public decimal TotalAmount { get; set; }

        // null = pagó el usuario
        public int? PayerPersonId { get; set; }
        public string? PayerPersonName { get; set; }

        public int? TransactionId { get; set; }
        public int? CardTransactionId { get; set; }
        public int? SharedExpenseId { get; set; }
        public string? Notes { get; set; }

        public List<SharedEventMovementShareDTO> Shares { get; set; } = new();
    }
}
