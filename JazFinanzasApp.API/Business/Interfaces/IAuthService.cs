using JazFinanzasApp.API.Business.DTO.User;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(RegisterUserDTO dto);
        Task<(bool Succeeded, string Token, string Message)> LoginAsync(LoginUserDTO dto);
    }
}
