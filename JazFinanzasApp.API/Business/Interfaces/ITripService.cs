using JazFinanzasApp.API.Business.DTO.Trip;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ITripService
    {
        Task<IEnumerable<TripDTO>> GetAllForUserAsync(int userId);
        Task<TripDTO> GetByIdAsync(int userId, int id);
        Task<TripDTO> CreateTripAsync(int userId, TripAddDTO dto);
        Task UpdateTripAsync(int userId, int id, TripEditDTO dto);
        Task DeleteTripAsync(int userId, int id);
    }
}
