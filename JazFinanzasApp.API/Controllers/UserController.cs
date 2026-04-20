using JazFinanzasApp.API.Business.DTO.User;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetUser()
        {
            var result = await _userService.GetUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser(EditUserDTO editUserDTO)
        {
            await _userService.UpdateUserAsync(GetUserId(), editUserDTO);
            return Ok(new { Message = "User updated successfully" });
        }

        [HttpPut("updatePassword")]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordDTO updatePasswordDTO)
        {
            await _userService.UpdatePasswordAsync(GetUserId(), updatePasswordDTO);
            return Ok(new { Message = "Password updated successfully" });
        }

        [HttpGet("getUserName")]
        public async Task<IActionResult> GetUserName()
        {
            var result = await _userService.GetUserNameAsync(GetUserId());
            return Ok(new { userName = result });
        }

        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO resetPasswordDTO)
        {
            await _userService.ResetPasswordAsync(GetUserId(), resetPasswordDTO);
            return Ok(new { Message = "Password reset successfully" });
        }
    }
}
