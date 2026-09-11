namespace JazFinanzasApp.API.Business.DTO.InvestmentReport
{
    // Carteras — General (Flujo 5): barras enfrentadas invertido vs valor actual + distribución + tabla.
    public class PortfoliosOverviewDTO
    {
        public string ReferenceAssetSymbol { get; set; }
        public List<PortfolioOverviewItemDTO> Portfolios { get; set; } = new();
    }

    public class PortfolioOverviewItemDTO
    {
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; }
        public bool IsDefault { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal? GainLossPercent { get; set; }
        // % del total de ActualValue de todas las carteras — la "distribución" del Flujo 5.
        public decimal SharePercent { get; set; }
    }

    // Carteras — Detalle: composición como mapa de bloques + evolución de su valor + tenencias con
    // drill-down por cuenta, en lugar del interruptor de agregado/desagregado (el reporte que hoy no
    // existe, 1.5 del plan).
    public class PortfolioDetailReportDTO
    {
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; }
        public string ReferenceAssetSymbol { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal? GainLossPercent { get; set; }
        public List<PortfolioHoldingItemDTO> Holdings { get; set; } = new();
        public List<InvestmentValuePointDTO> ValueSeries { get; set; } = new();
    }

    public class PortfolioHoldingItemDTO
    {
        public string AssetType { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public string AccountName { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal? GainLossPercent { get; set; }
        // Cotización de origen/actual (2026-09-10): ver PortfolioHoldingResult — calculadas en el
        // repositorio sobre valores sin redondear, para que el mismo activo (ej. ARS) muestre la
        // misma Cotización Actual sin importar en qué cuenta esté (antes, dividir OriginalValue/
        // ActualValue ya redondeados a 2 decimales por cuenta daba un valor levemente distinto en
        // cada fila para lo que en realidad es una única cotización de mercado).
        public decimal? OriginQuote { get; set; }
        public decimal? CurrentQuote { get; set; }
    }
}
