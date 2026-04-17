using JazFinanzasApp.API.Business.DTO.User;
using JazFinanzasApp.API.Infrastructure.Domain;
using Microsoft.AspNetCore.Identity;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<User> GetByIdAsync(int id);
        Task<User> GetByUserNameAsync(string userName);
        Task<string> GetUserNameByIdAsync(int id);
        Task<string> LoginUserAsync(LoginUserDTO model);
        Task<(IdentityResult Result, int UserId)> RegisterUserAsync(RegisterUserDTO model);
        Task<IdentityResult> ResetPasswordAsync(User user);
        Task<IdentityResult> UpdatePasswordAsync(User user, string oldPassword, string newPassword);
        Task<IdentityResult> UpdateUserAsync(User user);
    }
}
