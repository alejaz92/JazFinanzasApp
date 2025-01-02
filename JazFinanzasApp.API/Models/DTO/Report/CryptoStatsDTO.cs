namespace JazFinanzasApp.API.Models.DTO.Report
{
    public class CryptoStatsDTO
    {
        public CryptoStatsByDateDTO[] CryptoEvolutionStats { get; set; }

        public BalanceDTO[] CryptoBalanceStats { get; set; }

        public CryptoTransactionsStats[] CryptoTransactionsStats { get; set; }
    
    }

    public class CryptoTransactionsStats
    {
        public DateTime Date { get; set; }
        public string Account { get; set; }
        public string CommerceType { get; set; }

        public decimal Quantity { get; set; }
        public decimal QuotePrice { get; set; }
        public decimal Total { get; set; }
    }

}
