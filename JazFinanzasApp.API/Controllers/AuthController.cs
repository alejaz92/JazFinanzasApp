using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JazFinanzasApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly IUserRepository _userRepository;
        private readonly IMovementClassRepository _movementClassRepository;
        public AuthController(IUserRepository userRepository, IMovementClassRepository movementClassRepository)
        {
            _userRepository = userRepository;
            _movementClassRepository = movementClassRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDTO registerUserDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _userRepository.RegisterUserAsync(registerUserDTO);

            if (result.Result.Succeeded)
            {
                // al crear un usuario se le deben crear 2 clases de movimiento al usuario "Ajuste Saldo Ingreso", "Ajuste Saldo Egreso"

                var movementClassInc = new MovementClass
                {
                    Description = "Ajuste Saldos Ingreso",
                    IncExp = "I",
                    UserId = result.UserId
                };
                await _movementClassRepository.AddAsync(movementClassInc);

                var movementClassExp = new MovementClass
                {
                    Description = "Ajuste Saldos Egreso",
                    IncExp = "E",
                    UserId = result.UserId
                };
                await _movementClassRepository.AddAsync(movementClassExp);

                return Ok(new { Message = "User created succesfully" });
            }

            foreach (var error in result.Result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return BadRequest(ModelState);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserDTO loginUserDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = await _userRepository.LoginUserAsync(loginUserDTO);

            if (token == null)
            {
                return Unauthorized();
            }

            return Ok(new { Token = token });
        }       
    }
}
