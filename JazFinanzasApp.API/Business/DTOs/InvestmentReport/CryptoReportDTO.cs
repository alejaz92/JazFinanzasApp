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

    // Cryptos — Detalle: línea de cotización con compras/ventas marcadas encima y el precio
    // promedio de compra como línea horizontal.
    public class CryptoDetailReportDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public string ReferenceAssetSymbol { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public List<InvestmentValuePointDTO> PriceEvolution { get; set; } = new();
        public List<CryptoTransactionMarkerDTO> Transactions { get; set; } = new();
        public List<AccountHoldingAmountDTO> BalanceByAccount { get; set; } = new();
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
