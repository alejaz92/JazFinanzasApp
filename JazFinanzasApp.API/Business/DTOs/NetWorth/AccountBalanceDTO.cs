namespace JazFinanzasApp.API.Business.DTO.NetWorth
{
    public class AccountHoldingDTO
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public decimal NativeBalance { get; set; }
        public decimal BalanceInReferenceAsset { get; set; }
    }

    public class AccountBalanceDTO
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public List<MonthlyBalanceDTO> Evolution { get; set; } = new();
        public List<AccountHoldingDTO> Holdings { get; set; } = new();
    }
}
