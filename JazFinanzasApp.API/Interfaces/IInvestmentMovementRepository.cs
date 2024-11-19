using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IInvestmentMovementRepository : IGenericRepository<InvestmentMovement>
    {
        Task<InvestmentMovement> GetInvestmentMovementById(int id);
        Task<(IEnumerable<InvestmentMovement> Movements, int TotalCount)> GetPaginatedInvestmentMovements(int userId, int page, int pageSize, string environment);
    }
}
