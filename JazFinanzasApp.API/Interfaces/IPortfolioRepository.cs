using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IPortfolioRepository : IGenericRepository<Portfolio>
    {
        Task<bool> IsPortfolioUsedInTransactions(int portfolioId);
        Task<Portfolio> GetDefaultPortfolio(int userId);
    }
}
