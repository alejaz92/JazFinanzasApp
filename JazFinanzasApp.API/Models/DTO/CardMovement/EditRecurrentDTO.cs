namespace JazFinanzasApp.API.Models.DTO.CardMovement
{
    public class EditRecurrentDTO
    {
        public bool IsUpdate { get; set; }
        public DateTime Date { get; set; }
        public decimal? newAmount { get; set; }
        
    }
}
