namespace JazFinanzasApp.API.Business.DTO.Report
{
    public class HomeStatsDTO
    {
        public StocksGralStatsDTO[] StockStatsGral { get; set; }

        public StockStatsListDTO[] CryptoStatsGral { get; set; }
    }
}
