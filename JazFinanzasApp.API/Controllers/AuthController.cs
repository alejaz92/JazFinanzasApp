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
        private readonly IPortfolioRepository _portfolioRepository;
        public AuthController(IUserRepository userRepository, ITransactionClassRepository transactionClassRepository, IPortfolioRepository portfolioRepository)
        {
            _userRepository = userRepository;
            _transactionClassRepository = transactionClassRepository;
            _portfolioRepository = portfolioRepository;
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
                // Crear el portfolio por defecto al crear un usuario
                var portfolio = new Portfolio
                {
                    Name = "Default",
                    UserId = result.UserId,
                    IsDefault = true
                };
                await _portfolioRepository.AddAsync(portfolio);

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

        
    }
}
