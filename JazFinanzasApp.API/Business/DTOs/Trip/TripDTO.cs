namespace JazFinanzasApp.API.Business.DTO.Trip
{
    public class TripDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } // PLANNED / IN_PROGRESS / FINISHED (derivado de las fechas)
    }
}
