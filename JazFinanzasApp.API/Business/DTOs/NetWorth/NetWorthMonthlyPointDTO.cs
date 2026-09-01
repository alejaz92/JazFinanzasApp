namespace JazFinanzasApp.API.Business.DTO.NetWorth
{
    public class NetWorthMonthlyPointDTO
    {
        public DateTime Month { get; set; }
        public decimal Accounts { get; set; }
        public decimal Stocks { get; set; }
        public decimal CryptoStable { get; set; }
        public decimal CryptoVolatile { get; set; }
        public decimal Bonds { get; set; }
        public decimal Total => Accounts + Stocks + CryptoStable + CryptoVolatile + Bonds;
    }
}
