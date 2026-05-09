namespace JazFinanzasApp.API.Infrastructure.Data.QueryResults
{
    public class CryptoStatsByDateResult
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class CryptoStatsByDateCommerceResult
    {
        public DateTime Date { get; set; }
        public string CommerceType { get; set; }
        public decimal Value { get; set; }
    }
}
