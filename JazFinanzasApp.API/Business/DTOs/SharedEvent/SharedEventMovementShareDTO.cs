namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventMovementShareDTO
    {
        public int Id { get; set; }
        // null = la parte del usuario
        public int? PersonId { get; set; }
        public string? PersonName { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountSettled { get; set; }
        public decimal Pending { get; set; }
    }
}
