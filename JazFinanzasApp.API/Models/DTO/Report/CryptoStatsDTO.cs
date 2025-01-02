namespace JazFinanzasApp.API.Models.DTO.Report
{
    public class CryptoStatsDTO
    {
        public CryptoStatsByDateDTO[] CryptoEvolutionStats { get; set; }

        public BalanceDTO[] CryptoBalanceStats { get; set; }
    
    }

}
