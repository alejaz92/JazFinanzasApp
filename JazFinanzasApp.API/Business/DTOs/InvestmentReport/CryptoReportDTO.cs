namespace JazFinanzasApp.API.Business.DTO.InvestmentReport
{
    // Cryptos — General (Flujo 5): evolución del valor + compras por mes + distribución.
    public class CryptoOverviewReportDTO
    {
        public string ReferenceAssetSymbol { get; set; }
        public decimal TotalOriginalValue { get; set; }
        public decimal TotalActualValue { get; set; }
        public List<StockTickerReportDTO> Holdings { get; set; } = new();
        public List<InvestmentValuePointDTO> ValueEvolution { get; set; } = new();
        public List<CryptoPurchaseMonthDTO> PurchasesByMonth { get; set; } = new();
    }

    public class CryptoPurchaseMonthDTO
    {
        public DateTime Date { get; set; }
        public string CommerceType { get; set; }
        public decimal Value { get; set; }
    }

    // Detalle de un activo (T17, revisión de Bolsa 2026-09-12): línea de cotización con
    // compras/ventas marcadas encima y el precio promedio de compra como línea horizontal. Nació
    // como "Cryptos — Detalle" pero el cálculo no tiene nada de cripto adentro (es cotización, saldo
    // y movimientos por assetId) — se generalizó para que Bolsa — Detalle lo use igual, sin duplicar
    // el cálculo. El nombre de la clase quedó atrás de la migración: sigue siendo la respuesta de
    // `Crypto/{cryptoAssetId}/Detail/{assetId}` además de la nueva `Asset/{assetId}/Detail/{referenceAssetId}`.
    public class AssetDetailReportDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public string ReferenceAssetSymbol { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        // Ajustada por splits (T16, D-16): cada punto viejo se divide por el factor acumulado de los
        // splits posteriores a su fecha, para quedar en las mismas unidades que las marcas de
        // Transactions (que ya se ajustan en GetInvestmentsTransactionsStats) y que CurrentPrice.
        public List<InvestmentValuePointDTO> PriceEvolution { get; set; } = new();
        public List<CryptoTransactionMarkerDTO> Transactions { get; set; } = new();
        public List<AccountHoldingAmountDTO> BalanceByAccount { get; set; } = new();
        // D-16: fecha y ratio de cada split del activo, para que el frontend marque una línea
        // vertical y aclare en una leyenda por qué la escala de antes no es la que se ve en el
        // broker. Vacía para un activo sin splits (todos los de cripto, hoy).
        public List<AssetSplitEventMarkerDTO> SplitEvents { get; set; } = new();
        // D-15: la posición del usuario en este activo — null si nunca tuvo tenencia (ej. se llega
        // al detalle desde un ticker que ya está en 0 sin pasar por Bolsa — Detalle).
        public AssetPositionDTO Position { get; set; }
    }

    // D-16: una marca vertical en la línea de cotización.
    public class AssetSplitEventMarkerDTO
    {
        public DateTime Date { get; set; }
        public decimal SplitRatio { get; set; }
    }

    // D-15: cantidad, invertido, valor actual, ganancia/pérdida y peso dentro de su propia categoría
    // (Bolsa si es renta variable o fija, Cryptos si es cripto) — mismo criterio de "categoría" que
    // WeightPercent en StockTickerReportDTO.
    public class AssetPositionDTO
    {
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal? GainLossPercent { get; set; }
        public decimal WeightPercent { get; set; }
    }

    public class CryptoTransactionMarkerDTO
    {
        public DateTime Date { get; set; }
        public string Account { get; set; }
        // "I" compra, "E" venta (mismo criterio que Transaction.MovementType en toda la app).
        public string MovementType { get; set; }
        public string CommerceType { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuotePrice { get; set; }
        public decimal Total { get; set; }
    }

    public class AccountHoldingAmountDTO
    {
        public string Account { get; set; }
        public decimal Balance { get; set; }
    }
}
