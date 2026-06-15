using JazFinanzasApp.API.Business.DTO.Person;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class PersonService : IPersonService
    {
        private readonly IPersonRepository _personRepository;

        public PersonService(IPersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        public async Task<IEnumerable<PersonDTO>> GetAllForUserAsync(int userId)
        {
            var people = await _personRepository.GetByUserIdAsync(userId);
            return people.Select(p => new PersonDTO { Id = p.Id, Name = p.Name, Alias = p.Alias });
        }

        public async Task<PersonDTO> GetByIdAsync(int userId, int id)
        {
            var person = await _personRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Person not found");
            if (person.UserId != userId) throw new UnauthorizedDomainException();
            return new PersonDTO { Id = person.Id, Name = person.Name, Alias = person.Alias };
        }

        public async Task<PersonDTO> CreatePersonAsync(int userId, PersonAddDTO dto)
        {
            var existing = await _personRepository.FindAsync(p => p.Name == dto.Name && p.UserId == userId);
            if (existing.Any()) throw new BusinessRuleException("Ya existe una persona con ese nombre");

            var person = new Person { Name = dto.Name, Alias = dto.Alias, UserId = userId };
            var created = await _personRepository.AddAsyncReturnObject(person);
            return new PersonDTO { Id = created.Id, Name = created.Name, Alias = created.Alias };
        }

        public async Task UpdatePersonAsync(int userId, int id, PersonEditDTO dto)
        {
            var person = await _personRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Person not found");
            if (person.UserId != userId) throw new UnauthorizedDomainException();

            var duplicate = await _personRepository.FindAsync(p => p.Name == dto.Name && p.UserId == userId && p.Id != id);
            if (duplicate.Any()) throw new BusinessRuleException("Ya existe una persona con ese nombre");

            person.Name = dto.Name;
            person.Alias = dto.Alias;
            person.UpdatedAt = DateTime.UtcNow;
            await _personRepository.UpdateAsync(person);
        }

        public async Task DeletePersonAsync(int userId, int id)
        {
            var person = await _personRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Person not found");
            if (person.UserId != userId) throw new UnauthorizedDomainException();

            if (await _personRepository.HasActiveSplitsAsync(id))
                throw new BusinessRuleException("No se puede eliminar una persona con deudas pendientes");

            await _personRepository.DeleteAsync(id);
        }
    }
}
