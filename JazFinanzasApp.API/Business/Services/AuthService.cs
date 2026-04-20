using JazFinanzasApp.API.Business.DTO.User;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;

        public AuthService(
            IUserRepository userRepository,
            IPortfolioRepository portfolioRepository,
            ITransactionClassRepository transactionClassRepository)
        {
            _userRepository = userRepository;
            _portfolioRepository = portfolioRepository;
            _transactionClassRepository = transactionClassRepository;
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(RegisterUserDTO dto)
        {
            var result = await _userRepository.RegisterUserAsync(dto.Name, dto.LastName, dto.UserName, dto.Email, dto.Password);

            if (!result.Result.Succeeded)
                return (false, result.Result.Errors.Select(e => e.Description));

            await _portfolioRepository.AddAsync(new Portfolio
            {
                Name = "Default",
                UserId = result.UserId,
                IsDefault = true
            });

            await _transactionClassRepository.AddAsync(new TransactionClass { Description = "Ajuste Saldos Ingreso", IncExp = "I", UserId = result.UserId });
            await _transactionClassRepository.AddAsync(new TransactionClass { Description = "Ingreso Inversiones", IncExp = "I", UserId = result.UserId });
            await _transactionClassRepository.AddAsync(new TransactionClass { Description = "Ajuste Saldos Egreso", IncExp = "E", UserId = result.UserId });
            await _transactionClassRepository.AddAsync(new TransactionClass { Description = "Gastos Tarjeta", IncExp = "E", UserId = result.UserId });
            await _transactionClassRepository.AddAsync(new TransactionClass { Description = "Inversiones", IncExp = "E", UserId = result.UserId });

            return (true, Enumerable.Empty<string>());
        }

        public async Task<(bool Succeeded, string Token, string Message)> LoginAsync(LoginUserDTO dto)
        {
            var token = await _userRepository.LoginUserAsync(dto.UserName, dto.Password);

            if (token == null)
                return (false, null, "Invalid credentials");

            return (true, token, null);
        }
    }
}
