namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventBalanceDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }

        // null = el usuario
        public int? PersonId { get; set; }
        public string? PersonName { get; set; }

        public decimal Contributed { get; set; }
        public decimal Consumed { get; set; }
        public decimal NetBalance { get; set; }
    }
}
