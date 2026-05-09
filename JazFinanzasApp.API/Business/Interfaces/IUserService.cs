using JazFinanzasApp.API.Business.DTO.User;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IUserService
    {
        Task<EditUserDTO> GetUserAsync(int userId);
        Task UpdateUserAsync(int userId, EditUserDTO dto);
        Task UpdatePasswordAsync(int userId, UpdatePasswordDTO dto);
        Task<string> GetUserNameAsync(int userId);
        Task<string> ResetPasswordAsync(int adminUserId, ResetPasswordDTO dto);
    }
}
