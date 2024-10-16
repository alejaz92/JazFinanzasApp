namespace JazFinanzasApp.API.Models.DTO.Movement
{
    public class movementListDTO
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public string AccountName { get; set; }

        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public DateTime Date { get; set; }
        public string MovementType { get; set; }
        public int? MovementClassId { get; set; }
        public string? MovementClassName { get; set; }
        public string Detail { get; set; }
        public decimal Amount { get; set; }
    }
}
