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
    //
    // plan-alerta-cotizaciones, T2: AssetSymbol y AssetTypeName viajan igual que en StaleAssetResult
    // para que DashboardService arme el aviso de la bandeja sin otra consulta al repositorio.
    public class StaleAssetDTO
    {
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public string AssetTypeName { get; set; }
        public DateTime QuoteDate { get; set; }
    }

    public class NetWorthGeneralDTO
    {
        public List<NetWorthTotalDTO> Totals { get; set; } = new();
        public List<StaleAssetDTO> StaleAssets { get; set; } = new();
    }
}
