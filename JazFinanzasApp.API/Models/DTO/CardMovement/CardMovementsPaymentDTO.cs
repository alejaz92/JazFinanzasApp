namespace JazFinanzasApp.API.Models.DTO.CardMovement
{
    public class CardMovementsPaymentDTO
    {
        public DateTime Date { get; set; }
        public string MovementClass { get; set; }   
        public string Detail { get; set; }
        public string Asset { get; set; }
        public string Installment { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal ValueInPesos { get; set; }
    }
}
