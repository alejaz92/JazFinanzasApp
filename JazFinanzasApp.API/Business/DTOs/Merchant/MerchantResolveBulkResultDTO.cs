namespace JazFinanzasApp.API.Business.DTO.Merchant
{
    public class MerchantResolveBulkResultDTO
    {
        public int TransactionsResolved { get; set; }
        public int CardTransactionsResolved { get; set; }
        public int MerchantsCreated { get; set; }
    }
}
