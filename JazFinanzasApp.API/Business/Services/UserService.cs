using JazFinanzasApp.API.Business.DTO.User;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<EditUserDTO> GetUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found");
            return new EditUserDTO { Name = user.Name, LastName = user.LastName, Email = user.Email };
        }

        public async Task UpdateUserAsync(int userId, EditUserDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found");
            user.Name = dto.Name;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userRepository.UpdateUserAsync(user);
            if (!result.Succeeded)
                throw new BusinessRuleException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public async Task UpdatePasswordAsync(int userId, UpdatePasswordDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found");
            var passwordCheck = await _userRepository.CheckPasswordAsync(user, dto.OldPassword);
            if (!passwordCheck) throw new BusinessRuleException("Old password is incorrect");
            var result = await _userRepository.UpdatePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
                throw new BusinessRuleException(string.Join("; ", result.Errors.Select(e => e.Description)));
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);
        }

        public async Task<string> GetUserNameAsync(int userId)
        {
            var userName = await _userRepository.GetUserNameByIdAsync(userId)
                ?? throw new NotFoundException("User not found");
            return userName;
        }

        public async Task<string> ResetPasswordAsync(int adminUserId, ResetPasswordDTO dto)
        {
            var authorizedUser = await _userRepository.GetByIdAsync(adminUserId)
                ?? throw new NotFoundException("User not found");
            var roles = await _userRepository.GetRolesAsync(authorizedUser);
            if (!roles.Contains("Admin")) throw new UnauthorizedDomainException();

            var user = await _userRepository.GetByUserNameAsync(dto.userName)
                ?? throw new NotFoundException("Target user not found");

            var (result, tempPassword) = await _userRepository.ResetPasswordAsync(user);
            if (!result.Succeeded)
                throw new BusinessRuleException(string.Join("; ", result.Errors.Select(e => e.Description)));

            return tempPassword;
        }
    }
}
