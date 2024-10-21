using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetTypeRepository _assetTypeRepository;
        public AccountController(IAccountRepository accountRepository, IAssetTypeRepository assetTypeRepository)
        {
            _accountRepository = accountRepository;
            _assetTypeRepository = assetTypeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var accounts = await _accountRepository.GetByUserIdAsync(userId);

            // convert to DTO

            var accountsDTO = accounts.Select(a => new AccountDTO
            {
                Id = a.Id,
                Name = a.Name
            }).ToList();
            return Ok(accountsDTO);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }
            if(account.UserId != userId)
            {
                return Unauthorized();
            }

            var accountDTO = new AccountDTO
            {
                Name = account.Name
            };
            return Ok(accountDTO);
        }


        [HttpPost]
        public async Task<IActionResult> CreateAccount(AccountDTO accountDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var checkExists = await _accountRepository.FindAsync(a => a.Name == accountDTO.Name && a.UserId == userId);
            if(checkExists.Any())
            {
                return BadRequest("Account already exists");
            }

            var account = new Account
            {
                Name = accountDTO.Name,
                UserId = userId
            };

            await _accountRepository.AddAsync(account);

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, AccountDTO accountDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }
            if(account.UserId != userId)
            {
                return Unauthorized();
            }

            account.Name = accountDTO.Name;
            await _accountRepository.UpdateAsync(account);
            return Ok();
        }
        

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }
            if(account.UserId != userId)
            {
                return Unauthorized();
            }

            await _accountRepository.DeleteAsync(id);
            return Ok();
        }


        [HttpGet("byAssetType/{assetTypeName}")]
        public async Task<IActionResult> GetByAssetType(string assetTypeName)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var assetType = await _assetTypeRepository.GetByName(assetTypeName);
            if (assetType == null)
            {
                return NotFound();
            }
            

            var accounts = await _accountRepository.GetByAssetType(assetType.Id,userId);
            if (accounts == null)
            {
                return NotFound();
            }

            var accountsDTO = accounts.Select( a => new AccountDTO
            {
                Id = a.Id,
                Name = a.Name
            }).ToList();
            return Ok(accountsDTO);
        }
        
    }
}
