namespace JazFinanzasApp.API.Models.DTO.CardMovement
{
    public class EditRecurrentListDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Card { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime FirstInstallment { get; set; }
    }
}
