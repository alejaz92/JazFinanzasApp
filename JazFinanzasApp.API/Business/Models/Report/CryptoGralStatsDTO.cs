namespace JazFinanzasApp.API.Business.DTO.Report
{
    public class CryptoGralStatsDTO
    {
        public StockStatsListDTO[] CryptoGralStats { get; set; }
        public CryptoStatsByDateDTO[] CryptoStatsByDate { get; set; }
        public CryptoStatsByDateCommerceDTO[] CryptoPurchasesStatsByMonth { get; set; }
    }

    public class CryptoStatsByAssetDTO
    {
        public string AssetName { get; set; }
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal CurrentValue { get; set; }
    }

    public class CryptoStatsByDateDTO
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class CryptoStatsByDateCommerceDTO
    {
        public DateTime Date { get; set; }
        public string CommerceType { get; set; }
        public decimal Value { get; set; }
    }



}
