namespace JazFinanzasApp.API.Models.DTO.CardTransaction
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
