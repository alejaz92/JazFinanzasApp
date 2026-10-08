namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    // Panorama de inversiones (Fase 19): tenencia de un activo con su bucket de clasificación
    // (Stocks/Bonds/CryptoStable/CryptoVolatile, mismo criterio que TransactionRepository.ClassifyNetWorthBucket).
    public class InvestmentHoldingResult
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public string AssetTypeName { get; set; }
        public string Bucket { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        // plan-amortizaciones-bonos, T10: lo ya cobrado del activo (capital + interés de sus
        // BondCollection Registered/Untracked), en la moneda de referencia. 0 si no es un bono.
        public decimal CollectedValue { get; set; }
    }

    // Aportes vs rendimiento (Fase 19): aportes y retiros de un mes, en la moneda de referencia.
    public class InvestmentContributionMonthResult
    {
        public DateTime Month { get; set; }
        public decimal Contributed { get; set; }
        public decimal Withdrawn { get; set; }
    }

    // D-13 (revisión de Bolsa, 2026-09-12): un mes del entorno BOLSA, con su valor abierto por tipo
    // de activo en vez de los cinco buckets de NetWorthMonthlyPointResult.
    public class StocksMonthlyPointResult
    {
        public DateTime Month { get; set; }
        public List<AssetTypeValueResult> ByType { get; set; } = new();
    }

    public class AssetTypeValueResult
    {
        public string AssetTypeName { get; set; }
        public decimal Value { get; set; }
    }

    // D-14 (revisión de Bolsa, 2026-09-12): un activo de Bolsa con tenencia neta cero. RealizedResult
    // ya viene con el signo correcto (positivo = ganancia).
    public class ClosedPositionResult
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public string AssetTypeName { get; set; }
        // Ya incluye CollectedValue (T11): -OriginalValue + CollectedValue.
        public decimal RealizedResult { get; set; }
        public decimal CollectedValue { get; set; }
        public DateTime LastMovementDate { get; set; }
    }
}
