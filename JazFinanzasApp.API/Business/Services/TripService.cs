using JazFinanzasApp.API.Business.DTO.Trip;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class TripService : ITripService
    {
        private static readonly string[] ValidTypes = { "DOMESTIC", "INTERNATIONAL" };

        private readonly ITripRepository _tripRepository;

        public TripService(ITripRepository tripRepository)
        {
            _tripRepository = tripRepository;
        }

        public async Task<IEnumerable<TripDTO>> GetAllForUserAsync(int userId)
        {
            var trips = await _tripRepository.GetByUserIdAsync(userId);
            return trips.Select(MapToDTO);
        }

        public async Task<TripDTO> GetByIdAsync(int userId, int id)
        {
            var trip = await _tripRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Trip not found");
            if (trip.UserId != userId) throw new UnauthorizedDomainException();
            return MapToDTO(trip);
        }

        public async Task<TripDTO> CreateTripAsync(int userId, TripAddDTO dto)
        {
            ValidateFields(dto.Type, dto.StartDate, dto.EndDate);

            var existing = await _tripRepository.FindAsync(t => t.Name == dto.Name && t.UserId == userId);
            if (existing.Any()) throw new BusinessRuleException("Ya existe un viaje con ese nombre");

            var trip = new Trip
            {
                Name = dto.Name,
                Type = dto.Type,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                UserId = userId
            };
            var created = await _tripRepository.AddAsyncReturnObject(trip);
            return MapToDTO(created);
        }

        public async Task UpdateTripAsync(int userId, int id, TripEditDTO dto)
        {
            var trip = await _tripRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Trip not found");
            if (trip.UserId != userId) throw new UnauthorizedDomainException();

            ValidateFields(dto.Type, dto.StartDate, dto.EndDate);

            var duplicate = await _tripRepository.FindAsync(t => t.Name == dto.Name && t.UserId == userId && t.Id != id);
            if (duplicate.Any()) throw new BusinessRuleException("Ya existe un viaje con ese nombre");

            trip.Name = dto.Name;
            trip.Type = dto.Type;
            trip.StartDate = dto.StartDate;
            trip.EndDate = dto.EndDate;
            trip.UpdatedAt = DateTime.UtcNow;
            await _tripRepository.UpdateAsync(trip);
        }

        public async Task DeleteTripAsync(int userId, int id)
        {
            var trip = await _tripRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Trip not found");
            if (trip.UserId != userId) throw new UnauthorizedDomainException();

            await _tripRepository.DeleteAsync(id);
        }

        private static void ValidateFields(string type, DateTime startDate, DateTime endDate)
        {
            if (!ValidTypes.Contains(type))
                throw new BusinessRuleException("Tipo de viaje inválido");

            if (endDate.Date < startDate.Date)
                throw new BusinessRuleException("La fecha de fin no puede ser anterior a la fecha de inicio");
        }

        private static TripDTO MapToDTO(Trip trip)
        {
            return new TripDTO
            {
                Id = trip.Id,
                Name = trip.Name,
                Type = trip.Type,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Status = GetStatus(trip)
            };
        }

        private static string GetStatus(Trip trip)
        {
            var today = DateTime.UtcNow.Date;
            if (today < trip.StartDate.Date) return "PLANNED";
            if (today > trip.EndDate.Date) return "FINISHED";
            return "IN_PROGRESS";
        }
    }
}
