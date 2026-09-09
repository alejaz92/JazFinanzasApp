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
    }

    // Aportes vs rendimiento (Fase 19): aportes y retiros de un mes, en la moneda de referencia.
    public class InvestmentContributionMonthResult
    {
        public DateTime Month { get; set; }
        public decimal Contributed { get; set; }
        public decimal Withdrawn { get; set; }
    }
}
