namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventConsolidatedDebtDTO
    {
        public int PersonId { get; set; }
        public string PersonName { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }

        // a lo sumo uno de los dos es distinto de cero (posición neta consolidada: eventos + gastos sueltos V1)
        public decimal PendingInFavor { get; set; }
        public decimal PendingAgainst { get; set; }
    }
}
