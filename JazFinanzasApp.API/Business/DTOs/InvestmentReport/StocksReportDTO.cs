namespace JazFinanzasApp.API.Business.DTO.InvestmentReport
{
    // Bolsa (Flujo 5): barras divergentes de ganancia/pérdida por ticker + dispersión de rendimiento
    // contra peso en la cartera + tabla.
    public class StocksReportDTO
    {
        public string ReferenceAssetSymbol { get; set; }
        public decimal TotalOriginalValue { get; set; }
        public decimal TotalActualValue { get; set; }
        public List<StockTickerReportDTO> Tickers { get; set; } = new();
    }

    // Misma forma para Bolsa y para las tenencias de Cryptos — General (StockTickerReportDTO), ya
    // que ambos reportes muestran exactamente lo mismo por activo: cuánto tengo, cuánto puse, cuánto
    // vale y qué peso tiene dentro de su categoría.
    public class StockTickerReportDTO
    {
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal GainLossAmount { get; set; }
        public decimal? GainLossPercent { get; set; }
        // Peso dentro del total de su propia categoría (Bolsa o Cryptos), no del patrimonio entero.
        public decimal WeightPercent { get; set; }
    }
}
