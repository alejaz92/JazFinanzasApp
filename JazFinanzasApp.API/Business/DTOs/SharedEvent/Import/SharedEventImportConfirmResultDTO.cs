namespace JazFinanzasApp.API.Business.DTO.SharedEvent.Import
{
    public class SharedEventImportConfirmResultDTO
    {
        public int MovementsCreated { get; set; }
        public int PaymentsCreated { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
