using JazFinanzasApp.API.Business.DTO.Trip;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ITripService
    {
        Task<IEnumerable<TripDTO>> GetAllForUserAsync(int userId);
        Task<TripDetailDTO> GetByIdAsync(int userId, int id);
        Task<TripDTO> CreateTripAsync(int userId, TripAddDTO dto);
        Task UpdateTripAsync(int userId, int id, TripEditDTO dto);
        Task DeleteTripAsync(int userId, int id);
        Task AssociateMovementsAsync(int userId, int tripId, TripAssociationsDTO dto);
        Task DisassociateMovementsAsync(int userId, int tripId, TripAssociationsDTO dto);
        Task<IEnumerable<TripMovementDTO>> GetSuggestionsAsync(int userId, int tripId);
        Task DismissSuggestionAsync(int userId, int tripId, TripMovementRefDTO dto);
    }
}
