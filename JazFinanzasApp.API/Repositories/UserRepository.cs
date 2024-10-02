using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace JazFinanzasApp.API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly string _jwtSecret;
        private readonly IConfiguration configuration;

        public UserRepository(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _jwtSecret = configuration["Jwt:Key"];
            this.configuration = configuration;
        }

        public async Task<IdentityResult> RegisterUserAsync(RegisterUserDTO model)
        {
            var user = new User
            {
                Name = model.Name,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email
            };

            return await _userManager.CreateAsync(user, model.Password);
        }

        public async Task<string> LoginUserAsync(LoginUserDTO model)
        {
            var result = await _userManager.FindByNameAsync(model.UserName);
            if (result ==null)
            {
                return null; // usuario no encontrado
            }

            var passwordCheck = await _userManager.CheckPasswordAsync(result, model.Password);
            if(!passwordCheck)
            {
                return null; //contraseña incorrecta
            }
            return GenerateJwtToken(result);
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<User> GetByIdAsync(int id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public async Task<IdentityResult> UpdateUserAsync(User user)
        {
            return await _userManager.UpdateAsync(user);
        }
    }
}
