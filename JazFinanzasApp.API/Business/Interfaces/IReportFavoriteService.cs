using JazFinanzasApp.API.Business.DTO.ReportFavorite;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IReportFavoriteService
    {
        Task<IEnumerable<ReportFavoriteDTO>> GetAllForUserAsync(int userId);
        Task<ReportFavoriteDTO> CreateAsync(int userId, string reportKey);
        Task DeleteAsync(int userId, int id);
        Task ReorderAsync(int userId, List<int> orderedIds);
    }
}
