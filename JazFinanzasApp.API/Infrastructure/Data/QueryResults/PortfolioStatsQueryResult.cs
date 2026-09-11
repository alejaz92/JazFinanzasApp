namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class PortfolioStatsResult
    {
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; }
        public bool IsDefault { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
    }

    public class PortfolioHoldingResult
    {
        public string AssetType { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public string AccountName { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        // Cotización de origen/actual (2026-09-10): calculadas acá con los valores SIN redondear
        // (antes de Math.Round a 2 decimales para OriginalValue/ActualValue) — dividir dos valores
        // ya redondeados a 2 decimales por cuenta, como hacía el frontend, daba una "Cotización
        // Actual" ligeramente distinta para cada cuenta de un mismo activo (ej. ARS) el mismo día,
        // cuando en realidad hay una sola cotización de mercado. Null si Quantity es 0 (posición
        // cerrada, no hay "precio por unidad" que calcular).
        public decimal? OriginQuote { get; set; }
        public decimal? CurrentQuote { get; set; }
    }

    public class PortfolioValueByDateResult
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }
}
