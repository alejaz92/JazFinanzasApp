namespace JazFinanzasApp.API.Business.DTO.ReportFavorite
{
    public class ReportFavoriteDTO
    {
        public int Id { get; set; }
        public string ReportKey { get; set; }
        public int Order { get; set; }
    }

    public class CreateReportFavoriteDTO
    {
        public string ReportKey { get; set; }
    }
}
