using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class SharedEventService : ISharedEventService
    {
        private readonly ISharedEventRepository _sharedEventRepository;
        private readonly IPersonRepository _personRepository;

        public SharedEventService(ISharedEventRepository sharedEventRepository, IPersonRepository personRepository)
        {
            _sharedEventRepository = sharedEventRepository;
            _personRepository = personRepository;
        }

        public async Task<IEnumerable<SharedEventListDTO>> GetAllForUserAsync(int userId, bool includeClosed)
        {
            var events = await _sharedEventRepository.GetByUserIdAsync(userId, includeClosed);
            return events.Select(MapToListDTO);
        }

        public async Task<SharedEventDTO> GetByIdAsync(int userId, int id)
        {
            var sharedEvent = await GetOwnedDetailAsync(userId, id);
            return MapToDTO(sharedEvent);
        }

        public async Task<SharedEventDTO> CreateAsync(int userId, SharedEventAddDTO dto)
        {
            var personIds = dto.PersonIds.Distinct().ToList();
            await ValidatePersonsAsync(userId, personIds);

            var sharedEvent = new SharedEvent
            {
                Name = dto.Name,
                Notes = dto.Notes,
                UserId = userId,
                Participants = personIds.Select(pid => new SharedEventParticipant { PersonId = pid }).ToList()
            };

            var created = await _sharedEventRepository.AddAsyncReturnObject(sharedEvent);
            var full = await _sharedEventRepository.GetDetailByIdAsync(created.Id);
            return MapToDTO(full!);
        }

        public async Task UpdateAsync(int userId, int id, SharedEventEditDTO dto)
        {
            var sharedEvent = await GetOwnedEventAsync(userId, id);

            sharedEvent.Name = dto.Name;
            sharedEvent.Notes = dto.Notes;
            sharedEvent.UpdatedAt = DateTime.UtcNow;
            await _sharedEventRepository.UpdateAsync(sharedEvent);
        }

        public async Task<SharedEventDTO> AddParticipantAsync(int userId, int id, SharedEventParticipantAddDTO dto)
        {
            await GetOwnedEventAsync(userId, id);
            await ValidatePersonsAsync(userId, new List<int> { dto.PersonId });

            var existing = await _sharedEventRepository.GetParticipantAsync(id, dto.PersonId);
            if (existing != null)
                throw new BusinessRuleException("La persona ya es participante del evento");

            await _sharedEventRepository.AddParticipantAsync(new SharedEventParticipant
            {
                SharedEventId = id,
                PersonId = dto.PersonId
            });

            var full = await _sharedEventRepository.GetDetailByIdAsync(id);
            return MapToDTO(full!);
        }

        public async Task RemoveParticipantAsync(int userId, int id, int personId)
        {
            await GetOwnedEventAsync(userId, id);

            var participant = await _sharedEventRepository.GetParticipantAsync(id, personId)
                ?? throw new NotFoundException("La persona no es participante del evento");

            if (await _sharedEventRepository.ParticipantHasActivityAsync(id, personId))
                throw new BusinessRuleException("No se puede quitar un participante con movimientos o pagos en el evento");

            await _sharedEventRepository.RemoveParticipantAsync(participant);
        }

        public async Task CloseAsync(int userId, int id)
        {
            var sharedEvent = await GetOwnedEventAsync(userId, id);
            if (sharedEvent.IsClosed)
                throw new BusinessRuleException("El evento ya está cerrado");

            // Fase 2: todavía no existen movimientos/pagos (Fases 3-4), así que no hay balances
            // que validar. La validación completa de saldos en cero (D4) se completa en la Fase 4.
            if (await _sharedEventRepository.HasMovementsOrPaymentsAsync(id))
                throw new BusinessRuleException("El cierre con movimientos o pagos pendientes se habilita en una fase posterior");

            sharedEvent.IsClosed = true;
            sharedEvent.UpdatedAt = DateTime.UtcNow;
            await _sharedEventRepository.UpdateAsync(sharedEvent);
        }

        public async Task ReopenAsync(int userId, int id)
        {
            var sharedEvent = await GetOwnedEventAsync(userId, id);

            sharedEvent.IsClosed = false;
            sharedEvent.UpdatedAt = DateTime.UtcNow;
            await _sharedEventRepository.UpdateAsync(sharedEvent);
        }

        public async Task DeleteAsync(int userId, int id)
        {
            await GetOwnedEventAsync(userId, id);

            if (await _sharedEventRepository.HasMovementsOrPaymentsAsync(id))
                throw new BusinessRuleException("No se puede eliminar un evento con movimientos o pagos");

            await _sharedEventRepository.DeleteEventWithParticipantsAsync(id);
        }

        private async Task ValidatePersonsAsync(int userId, List<int> personIds)
        {
            foreach (var personId in personIds)
            {
                var person = await _personRepository.GetByIdAsync(personId)
                    ?? throw new NotFoundException($"Persona {personId} no encontrada");
                if (person.UserId != userId)
                    throw new UnauthorizedDomainException();
            }
        }

        private async Task<SharedEvent> GetOwnedEventAsync(int userId, int id)
        {
            var sharedEvent = await _sharedEventRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Evento compartido no encontrado");
            if (sharedEvent.UserId != userId) throw new UnauthorizedDomainException();
            return sharedEvent;
        }

        private async Task<SharedEvent> GetOwnedDetailAsync(int userId, int id)
        {
            var sharedEvent = await _sharedEventRepository.GetDetailByIdAsync(id)
                ?? throw new NotFoundException("Evento compartido no encontrado");
            if (sharedEvent.UserId != userId) throw new UnauthorizedDomainException();
            return sharedEvent;
        }

        private static SharedEventListDTO MapToListDTO(SharedEvent e)
        {
            return new SharedEventListDTO
            {
                Id = e.Id,
                Name = e.Name,
                IsClosed = e.IsClosed,
                ParticipantCount = e.Participants?.Count ?? 0,
                MovementCount = e.Movements?.Count ?? 0
            };
        }

        private static SharedEventDTO MapToDTO(SharedEvent e)
        {
            return new SharedEventDTO
            {
                Id = e.Id,
                Name = e.Name,
                Notes = e.Notes,
                IsClosed = e.IsClosed,
                Participants = e.Participants?
                    .OrderBy(p => p.Person?.Alias ?? p.Person?.Name)
                    .Select(p => new SharedEventParticipantDTO
                    {
                        PersonId = p.PersonId,
                        PersonName = p.Person?.Alias ?? p.Person?.Name ?? string.Empty
                    }).ToList() ?? new()
            };
        }
    }
}
