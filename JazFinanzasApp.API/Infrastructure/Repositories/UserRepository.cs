using JazFinanzasApp.API.Business.DTO.User;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace JazFinanzasApp.API.Infrastructure.Repositories
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

        public async Task<(IdentityResult Result, int UserId)> RegisterUserAsync(RegisterUserDTO model)
        {
            var user = new User
            {
                Name = model.Name,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email
            };

            var result =  await _userManager.CreateAsync(user, model.Password);

            return (result, user.Id);
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

       
        public async Task<IdentityResult> UpdatePasswordAsync(User user, string oldPassword, string newPassword)
        {
            return await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        }

        
        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

       
        public async Task<string> GetUserNameByIdAsync(int id)
        {
            var user = await GetByIdAsync(id);
            return user.UserName;
        }

        public async Task<User> GetByUserNameAsync(string userName) => await _userManager.FindByNameAsync(userName);


        public async Task<IdentityResult> ResetPasswordAsync(User user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return await _userManager.ResetPasswordAsync(user, token, user.UserName + "123456");
        }

    }
}
