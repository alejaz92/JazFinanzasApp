namespace JazFinanzasApp.API.Business.DTO.NetWorth
{
    public class NetWorthTotalDTO
    {
        public string Asset { get; set; }
        public string Symbol { get; set; }
        public string Color { get; set; }
        public decimal GrossBalance { get; set; }
        public decimal CardDebt { get; set; }
        public decimal NetBalance { get; set; }
    }

    // T9: qué tenencias de hoy están valuadas con una cotización vieja — no es propiedad de
    // ninguna moneda de referencia en particular, por eso viaja aparte y una sola vez.
    public class StaleAssetDTO
    {
        public string AssetName { get; set; }
        public DateTime QuoteDate { get; set; }
    }

    public class NetWorthGeneralDTO
    {
        public List<NetWorthTotalDTO> Totals { get; set; } = new();
        public List<StaleAssetDTO> StaleAssets { get; set; } = new();
    }
}
