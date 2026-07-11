namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventMovementShareInputDTO
    {
        // null = la parte del usuario
        public int? PersonId { get; set; }
        public decimal Amount { get; set; }
    }
}
