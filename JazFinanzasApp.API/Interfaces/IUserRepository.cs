using JazFinanzasApp.API.Models.DTO.User;
using Microsoft.AspNetCore.Identity;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IUserRepository
    {
        Task<string> LoginUserAsync(LoginUserDTO model);
        Task<IdentityResult> RegisterUserAsync(RegisterUserDTO model);
    }
}
