using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.DTO.Account_AssetType;
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

            var result = await _account_AssetTypeRepository.GetAssetTypes(id);
            return Ok(result);
        }
    }
}   
