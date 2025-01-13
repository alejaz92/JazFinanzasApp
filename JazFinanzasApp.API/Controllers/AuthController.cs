using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.User;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ITransactionClassRepository _transactionClassRepository;
        public AuthController(IUserRepository userRepository, ITransactionClassRepository transactionClassRepository)
        {
            _userRepository = userRepository;
            _transactionClassRepository = transactionClassRepository;
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

                var transactionClassInc = new TransactionClass
                {
                    Description = "Ajuste Saldos Ingreso",
                    IncExp = "I",
                    UserId = result.UserId
                };
                await _transactionClassRepository.AddAsync(transactionClassInc);

                transactionClassInc = new TransactionClass
                {
                    Description = "Ingreso Inversiones",
                    IncExp = "I",
                    UserId = result.UserId
                };
                await _transactionClassRepository.AddAsync(transactionClassInc);


                var transactionClassExp = new TransactionClass
                {
                    Description = "Ajuste Saldos Egreso",
                    IncExp = "E",
                    UserId = result.UserId
                };
                await _transactionClassRepository.AddAsync(transactionClassExp);


                transactionClassExp = new TransactionClass
                {
                    Description = "Gastos Tarjeta",
                    IncExp = "E",
                    UserId = result.UserId
                };
                await _transactionClassRepository.AddAsync(transactionClassExp);

                transactionClassExp = new TransactionClass
                {
                    Description = "Inversiones",
                    IncExp = "E",
                    UserId = result.UserId
                };
                await _transactionClassRepository.AddAsync(transactionClassExp);


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

        [HttpPost("reset-password")]
        [Authorize]
        public async Task<IActionResult> ResetPassword([FromBody] string userName)
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
