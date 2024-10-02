using JazFinanzasApp.API.Interfaces;
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
                Id = user.Id,
                Name = user.Name,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email
            };

            return Ok(userDTO);
        }

        //metodo put, solo se pueda actualizar name, lastname y email
        [HttpPut]
        public async Task<IActionResult> UpdateUser(EditUserDTO editUserDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            if (userId != editUserDTO.Id)
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.Name = editUserDTO.Name;
            user.LastName = editUserDTO.LastName;
            user.Email = editUserDTO.Email;

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
    }
}
