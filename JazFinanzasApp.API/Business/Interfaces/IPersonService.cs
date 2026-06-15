using JazFinanzasApp.API.Business.DTO.Person;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IPersonService
    {
        Task<IEnumerable<PersonDTO>> GetAllForUserAsync(int userId);
        Task<PersonDTO> GetByIdAsync(int userId, int id);
        Task<PersonDTO> CreatePersonAsync(int userId, PersonAddDTO dto);
        Task UpdatePersonAsync(int userId, int id, PersonEditDTO dto);
        Task DeletePersonAsync(int userId, int id);
    }
}
