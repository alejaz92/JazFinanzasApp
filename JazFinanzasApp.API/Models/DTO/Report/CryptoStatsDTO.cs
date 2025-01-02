namespace JazFinanzasApp.API.Models.DTO.Report
{
    public class CryptoStatsDTO
    {
        public CryptoStatsByDateDTO[] CryptoEvolutionStats { get; set; }

        public BalanceDTO[] CryptoBalanceStats { get; set; }

        public InvestmentTransactionsStatsDTO[] CryptoTransactionsStats { get; set; }

        public InvestmentRangeValuesStatsDTO CryptoRangeValuesStats { get; set; }
    
    }

    public class InvestmentTransactionsStatsDTO
    {
        public DateTime Date { get; set; }
        public string Account { get; set; }
        public string MovementType { get; set; }   
        public string CommerceType { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuotePrice { get; set; }
        public decimal Total { get; set; }
    }

    public class InvestmentRangeValuesStatsDTO
    {
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public decimal CurrentValue { get; set; }
    }                       

}
