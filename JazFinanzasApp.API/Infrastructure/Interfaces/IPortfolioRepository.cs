using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IPortfolioRepository : IGenericRepository<Portfolio>
    {
        Task<bool> IsPortfolioUsedInTransactions(int portfolioId);
        Task<Portfolio> GetDefaultPortfolio(int userId);
        Task<IEnumerable<Portfolio>> GetByUserIdAsync(int userId);
    }
}
