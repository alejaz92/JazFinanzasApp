using JazFinanzasApp.API.Business.DTO.Dashboard;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDTO> GetDashboardAsync(int userId, int assetId);
    }
}
