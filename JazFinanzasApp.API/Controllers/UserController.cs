using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetUser()
        {
           var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var userDTO = new EditUserDTO
            {
                Name = user.Name,
                LastName = user.LastName,
                Email = user.Email
            };

            return Ok(userDTO);
        }

       
        [HttpPut]
        public async Task<IActionResult> UpdateUser(EditUserDTO editUserDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.Name = editUserDTO.Name;
            user.LastName = editUserDTO.LastName;
            user.Email = editUserDTO.Email;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userRepository.UpdateUserAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Message = "User updated succesfully" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return BadRequest(ModelState);
        }

        
        [HttpPut("updatePassword")]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordDTO updatePasswordDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var passwordCheck = await _userRepository.CheckPasswordAsync(user, updatePasswordDTO.OldPassword);

            if (!passwordCheck)
            {
                return BadRequest(new { Message = "Old password is incorrect" });
            }

            var result = await _userRepository.UpdatePasswordAsync(user, updatePasswordDTO.OldPassword, updatePasswordDTO.NewPassword);

            if (result.Succeeded)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(user);

                return Ok(new { Message = "Password updated succesfully" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
                Console.WriteLine($"Code: {error.Code}, Description: {error.Description}");
            }

            return BadRequest(ModelState);
        }

        [HttpGet("getUserName")]
        public async Task<IActionResult> GetUserName()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var userName = await _userRepository.GetUserNameByIdAsync(userId);

            if (userName == null)
            {
                return NotFound();
            }

            return Ok(new { UserName = userName });
        }

        [HttpPost("reset-password")]
        [Authorize]
        public async Task<IActionResult> ResetPassword([FromHeader] string userName)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            User authorizedUser = await _userRepository.GetByIdAsync(userId);
            if (authorizedUser.UserName != "ajazmatie")
            {
                return Unauthorized();
            }

            User user = await _userRepository.GetByUserNameAsync(userName);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userRepository.ResetPasswordAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Password reset succesfully to: " + user.UserName + "123456" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return BadRequest(ModelState);
        }

    }
}
