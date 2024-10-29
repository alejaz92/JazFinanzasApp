using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IMovementClassRepository : IGenericRepository<MovementClass>
    {
        Task<MovementClass> GetMovementClassByDescriptionAsync(string Description);
    }
}
