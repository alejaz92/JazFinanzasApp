using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IPersonRepository : IGenericRepository<Person>
    {
        Task<IEnumerable<Person>> GetByUserIdAsync(int userId);
        Task<bool> HasActiveSplitsAsync(int personId);
    }
}
