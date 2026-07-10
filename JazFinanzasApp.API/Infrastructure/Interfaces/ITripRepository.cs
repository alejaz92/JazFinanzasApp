using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ITripRepository : IGenericRepository<Trip>
    {
        Task<IEnumerable<Trip>> GetByUserIdAsync(int userId);
    }
}
