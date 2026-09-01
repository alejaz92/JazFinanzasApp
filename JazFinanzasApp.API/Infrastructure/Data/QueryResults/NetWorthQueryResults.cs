namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class MonthlyBalanceResult
    {
        public DateTime Month { get; set; }
        public decimal Balance { get; set; }
    }

    // T9: un activo que hoy se tiene en cartera pero cuya última cotización supera el umbral de
    // frescura — no es "el total de hoy no es de hoy", es "esta tenencia puntual está vieja".
    public class StaleAssetResult
    {
        public string AssetName { get; set; }
        public DateTime QuoteDate { get; set; }
    }

    // Apertura del patrimonio por gran grupo de activo (D-6 / Flujo 2), un punto por mes.
    public class NetWorthMonthlyPointResult
    {
        public DateTime Month { get; set; }
        public decimal Accounts { get; set; }
        public decimal Stocks { get; set; }
        public decimal Crypto { get; set; }
        public decimal Bonds { get; set; }
    }

    public class AccountHoldingResult
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public decimal NativeBalance { get; set; }
        public decimal BalanceInReferenceAsset { get; set; }
    }

    public class AccountBalanceResult
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public List<MonthlyBalanceResult> Evolution { get; set; } = new();
        public List<AccountHoldingResult> Holdings { get; set; } = new();
    }

    // "Pesos" vs "Dolarizado" (D-6bis: todo lo que no está en Peso Argentino protege contra la devaluación).
    public class CurrencyExposureResult
    {
        public string Label { get; set; }
        public decimal Balance { get; set; }
    }
}
