namespace JazFinanzasApp.API.Business.DTO.Transaction
{
    public class RefundDTO
    {
        public int AccountId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public List<SplitAllocationDTO>? SplitAllocations { get; set; }
    }

    public class SplitAllocationDTO
    {
        public int SplitId { get; set; }
        public decimal Amount { get; set; }
    }
}
