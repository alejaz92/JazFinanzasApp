using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IMovementRepository : IGenericRepository<Movement>
    {
        Task<Movement> GetMovementByIdAsync(int id);
        Task<(IEnumerable<Movement> Movements, int TotalCount)> GetPaginatedMovements(int userId, int page, int pageSize);
    }
}
