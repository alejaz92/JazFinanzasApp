namespace JazFinanzasApp.API.Business.DTO.InvestmentReport
{
    // Panorama de inversiones (Flujo 5, Fase 19): mapa de bloques de todo lo invertido + línea del
    // valor total con los aportes marcados encima.
    public class InvestmentOverviewDTO
    {
        public string ReferenceAssetSymbol { get; set; }
        public decimal TotalOriginalValue { get; set; }
        public decimal TotalActualValue { get; set; }
        public decimal? GainLossPercent { get; set; }
        public List<InvestmentHoldingDTO> Holdings { get; set; } = new();
        public List<InvestmentValuePointDTO> ValueSeries { get; set; } = new();
        public List<InvestmentContributionMarkerDTO> ContributionMarkers { get; set; } = new();
    }

    public class InvestmentHoldingDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        // Stocks | Bonds | CryptoStable | CryptoVolatile (mismo criterio que Patrimonio, Fases 10/16).
        public string Bucket { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal? GainLossPercent { get; set; }
    }

    // Un punto genérico Mes/Valor — lo reusan todas las evoluciones mensuales de Inversiones
    // (línea de Panorama, evolución de cartera, evolución de cripto).
    public class InvestmentValuePointDTO
    {
        public DateTime Month { get; set; }
        public decimal Value { get; set; }
    }

    public class InvestmentContributionMarkerDTO
    {
        public DateTime Month { get; set; }
        public decimal Contributed { get; set; }
        public decimal Withdrawn { get; set; }
    }
}
