using JazFinanzasApp.API.Business.DTO.User;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JazFinanzasApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDTO registerUserDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(registerUserDTO);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);
                return BadRequest(ModelState);
            }
            return Ok(new { Message = "User created succesfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserDTO loginUserDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.LoginAsync(loginUserDTO);
            if (!result.Succeeded) return Unauthorized();
            return Ok(new { Token = result.Token });
        }
    }
}

