namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventCategoryTotalDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public int TransactionClassId { get; set; }
        public string TransactionClassName { get; set; }
        public decimal Total { get; set; }
    }
}
