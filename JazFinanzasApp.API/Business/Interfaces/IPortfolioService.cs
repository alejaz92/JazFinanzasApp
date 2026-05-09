using JazFinanzasApp.API.Business.DTO.Portfolio;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IPortfolioService
    {
        Task<IEnumerable<PortfolioDTO>> GetAllForUserAsync(int userId);
        Task<PortfolioDTO> GetByIdAsync(int userId, int id);
        Task CreatePortfolioAsync(int userId, PortfolioDTO dto);
        Task UpdatePortfolioAsync(int userId, int id, PortfolioDTO dto);
        Task DeletePortfolioAsync(int userId, int id);
        Task<PortfolioDTO> GetDefaultPortfolioAsync(int userId);
    }
}
