namespace JazFinanzasApp.API.Business.DTO.InvestmentReport
{
    // Bolsa — General (revisión 2026-09-12, Fase 20a): mapa de bloques en dos niveles (tipo → ticker)
    // + evolución de 12 meses por tipo + barras divergentes por tipo + tabla agrupada. D-10: cubre
    // todo el entorno BOLSA (renta variable y renta fija), no solo el bucket "Stocks" como antes.
    public class StocksReportDTO
    {
        public string ReferenceAssetSymbol { get; set; }
        public decimal TotalOriginalValue { get; set; }
        public decimal TotalActualValue { get; set; }
        // Agregados por tipo de activo, siempre sobre el entorno completo — el filtro de la barra
        // (D-11) recorta Tickers, no esta lista, para que el combo pueda mostrar todos los tipos
        // aunque se esté mirando uno solo.
        public List<StockTypeAggregateDTO> Types { get; set; } = new();
        public List<StockTickerReportDTO> Tickers { get; set; } = new();
        // D-13: valor mensual de los últimos 12 meses, abierto por tipo de activo.
        public List<StocksMonthlyPointDTO> ValueSeries { get; set; } = new();
        // D-14: posiciones ya vendidas del todo, con su resultado realizado. Vacía salvo que se pida
        // con includeClosed=true (apagado por default).
        public List<ClosedPositionDTO> ClosedPositions { get; set; } = new();
    }

    // Misma forma para Bolsa y para las tenencias de Cryptos — General (StockTickerReportDTO), ya
    // que ambos reportes muestran exactamente lo mismo por activo: cuánto tengo, cuánto puse, cuánto
    // vale y qué peso tiene dentro de su categoría.
    public class StockTickerReportDTO
    {
        // AssetId y AssetTypeName (revisión 2026-09-12): habilitan el enlace a Bolsa — Detalle y el
        // filtro/agrupación por tipo de activo (D-11) — los dos datos ya venían en
        // InvestmentHoldingResult y se descartaban al armar esta lista.
        public int AssetId { get; set; }
        public string AssetTypeName { get; set; }
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

    // D-12: reemplaza al ranking de las 30 barras — cuatro o cinco barras, una por tipo de activo.
    public class StockTypeAggregateDTO
    {
        public string AssetTypeName { get; set; }
        public int TickerCount { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal? GainLossPercent { get; set; }
    }

    // D-13: un mes, con el valor de cada tipo de activo dentro de Bolsa — la contraparte de
    // InvestmentValuePointDTO (un solo valor) cuando hace falta abrir por categoría.
    public class StocksMonthlyPointDTO
    {
        public DateTime Month { get; set; }
        public List<AssetTypeValueDTO> ByType { get; set; } = new();
    }

    public class AssetTypeValueDTO
    {
        public string AssetTypeName { get; set; }
        public decimal Value { get; set; }
    }

    // D-14: una posición de Bolsa con tenencia neta cero. RealizedResult ya viene con el signo
    // correcto (positivo = ganancia), calculado con la cotización de cada movimiento (T6), nunca en
    // pesos nominales.
    public class ClosedPositionDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public string AssetTypeName { get; set; }
        public decimal RealizedResult { get; set; }
        public DateTime LastMovementDate { get; set; }
    }
}
