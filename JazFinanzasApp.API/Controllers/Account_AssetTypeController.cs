using JazFinanzasApp.API.Business.DTO.Account_AssetType;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Account_AssetTypeController : ControllerBase
    {
        private readonly IAccount_AssetTypeRepository _account_AssetTypeRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetTypeRepository _assetTypeRepository;
        public Account_AssetTypeController(IAccount_AssetTypeRepository account_AssetTypeRepository,
            IAccountRepository accountRepository, IAssetTypeRepository assetTypeRepository)
        {
            _account_AssetTypeRepository = account_AssetTypeRepository;
            _accountRepository = accountRepository;
            _assetTypeRepository = assetTypeRepository;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssetTypes(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            if (account.UserId != int.Parse(userIdClaim.Value))
            {
                return Unauthorized();
            }

            var result = await _account_AssetTypeRepository.GetAssetTypes(id);
            return Ok(result);
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> AssignAssetTypesToAccount(int id, [FromBody]List<Account_AssetTypeDTO> assetTypes)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            if (account.UserId != int.Parse(userIdClaim.Value))
            {
                return Unauthorized();
            }

            if (assetTypes == null || !assetTypes.Any())
            {
                return BadRequest("Select at least 1");
            }

            var result = await _account_AssetTypeRepository.AssignAssetTypesToAccountAsync(id, assetTypes);
            return Ok(result);
        }
    }
}   
