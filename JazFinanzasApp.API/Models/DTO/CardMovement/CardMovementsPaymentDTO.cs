using JazFinanzasApp.API.Models.DTO.CardMovement;

namespace JazFinanzasApp.API.Models
{
    public class CardMovementsPaymentDTO
    {
        public int CardId { get; set; }
        public DateTime PaymentMonth { get; set; }
        public DateTime PaymentDate { get; set; }
        public int accountId { get; set; }
        public string PaymentAsset { get; set; }
        public decimal PesosAmount { get; set; }
        public decimal? DolarAmount { get; set; }
        public decimal CardExpenses { get; set; }

        public List<CardMovementsPaymentListDTO> CardMovements { get; set; }
     
    }
}
