using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IReportFavoriteRepository : IGenericRepository<ReportFavorite>
    {
        Task<IEnumerable<ReportFavorite>> GetByUserIdAsync(int userId);
        Task<ReportFavorite> GetByReportKeyAsync(string reportKey, int userId);
    }
}
