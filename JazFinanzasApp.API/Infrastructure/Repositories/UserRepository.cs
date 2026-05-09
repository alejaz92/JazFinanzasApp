using JazFinanzasApp.API.Domain;
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

        public async Task<(IdentityResult Result, int UserId)> RegisterUserAsync(string name, string lastName, string userName, string email, string password)
        {
            var user = new User
            {
                Name = name,
                LastName = lastName,
                UserName = userName,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);

            return (result, user.Id);
        }

        public async Task<string> LoginUserAsync(string userName, string password)
        {
            var result = await _userManager.FindByNameAsync(userName);
            if (result ==null)
            {
                return null; // usuario no encontrado
            }

            var passwordCheck = await _userManager.CheckPasswordAsync(result, password);
            if(!passwordCheck)
            {
                return null; //contrase�a incorrecta
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


        public async Task<(IdentityResult Result, string TempPassword)> ResetPasswordAsync(User user)
        {
            var tempPassword = GenerateSecurePassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);
            return (result, tempPassword);
        }

        private static string GenerateSecurePassword()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            const string all = upper + lower + digits + special;

            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[16];
            rng.GetBytes(bytes);

            var chars = new char[12];
            chars[0] = upper[bytes[0] % upper.Length];
            chars[1] = digits[bytes[1] % digits.Length];
            chars[2] = special[bytes[2] % special.Length];
            chars[3] = lower[bytes[3] % lower.Length];
            for (int i = 4; i < 12; i++)
                chars[i] = all[bytes[i] % all.Length];

            return new string(chars.OrderBy(_ => System.Security.Cryptography.RandomNumberGenerator.GetInt32(100)).ToArray());
        }

        public async Task<IList<string>> GetRolesAsync(User user)
        {
            return await _userManager.GetRolesAsync(user);
        }

    }
}
