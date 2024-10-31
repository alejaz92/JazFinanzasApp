
namespace JazFinanzasApp.API.Models.DTO.CardMovement
{
    public class CardMovementsPendingDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Card  { get; set; }
        public string MovementClass { get; set; }          
        public string Detail { get; set; }
        public string Installments { get; set; }
        public string Asset { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime FirstInstallment { get; set; }
        public string LastInstallment { get; set; }
        public decimal InstallmentAmount { get; set; }

    }
}
