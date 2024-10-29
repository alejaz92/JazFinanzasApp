namespace JazFinanzasApp.API.Models.DTO.CardMovement
{
    public class CardMovementsPaymentListDTO
    {
        public DateTime Date { get; set; }
        public int MovementClassId { get; set; }
        public string MovementClass { get; set; }   
        public string Detail { get; set; }
        public int AssetId { get; set; }
        public string Asset { get; set; }
        public string Installment { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal ValueInPesos { get; set; }
    }
}
