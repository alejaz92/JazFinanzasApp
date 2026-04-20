using JazFinanzasApp.API.Domain;
using Microsoft.AspNetCore.Identity;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<User> GetByIdAsync(int id);
        Task<User> GetByUserNameAsync(string userName);
        Task<string> GetUserNameByIdAsync(int id);
        Task<string> LoginUserAsync(string userName, string password);
        Task<(IdentityResult Result, int UserId)> RegisterUserAsync(string name, string lastName, string userName, string email, string password);
        Task<IdentityResult> ResetPasswordAsync(User user);
        Task<IdentityResult> UpdatePasswordAsync(User user, string oldPassword, string newPassword);
        Task<IdentityResult> UpdateUserAsync(User user);
    }
}
