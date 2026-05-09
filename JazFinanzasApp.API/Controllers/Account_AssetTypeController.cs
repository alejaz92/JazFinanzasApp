using JazFinanzasApp.API.Business.DTO.Account_AssetType;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Account_AssetTypeController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public Account_AssetTypeController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssetTypes(int id)
        {
            var result = await _accountService.GetAssetTypesForAccountAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> AssignAssetTypesToAccount(int id, [FromBody] List<Account_AssetTypeDTO> assetTypes)
        {
            await _accountService.AssignAssetTypesToAccountAsync(GetUserId(), id, assetTypes);
            return Ok();
        }
    }
}
