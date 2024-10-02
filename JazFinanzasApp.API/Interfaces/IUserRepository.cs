using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.User;
using Microsoft.AspNetCore.Identity;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(int id);
        Task<string> LoginUserAsync(LoginUserDTO model);
        Task<IdentityResult> RegisterUserAsync(RegisterUserDTO model);
        Task<IdentityResult> UpdateUserAsync(User user);
    }
}
