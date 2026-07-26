namespace JazFinanzasApp.API.Business.DTO.Card
{
    public class CardDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? NextClosingDate { get; set; }
        public DateTime? NextDueDate { get; set; }
        public bool IsCurrentPeriodPaid { get; set; }
    }
}
