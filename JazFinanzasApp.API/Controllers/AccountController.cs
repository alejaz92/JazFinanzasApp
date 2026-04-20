using JazFinanzasApp.API.Business.DTO.Account;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var result = await _accountService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _accountService.GetByIdAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(AccountDTO accountDTO)
        {
            await _accountService.CreateAccountAsync(GetUserId(), accountDTO);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, AccountDTO accountDTO)
        {
            await _accountService.UpdateAccountAsync(GetUserId(), id, accountDTO);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            await _accountService.DeleteAccountAsync(GetUserId(), id);
            return Ok();
        }

        [HttpGet("byAssetType/{assetTypeName}")]
        public async Task<IActionResult> GetByAssetType(string assetTypeName)
        {
            var result = await _accountService.GetByAssetTypeNameAsync(GetUserId(), assetTypeName);
            return Ok(result);
        }

        [HttpGet("byAssetTypeId/{assetTypeId}")]
        public async Task<IActionResult> GetByAssetTypeId(int assetTypeId)
        {
            var result = await _accountService.GetByAssetTypeAsync(GetUserId(), assetTypeId);
            return Ok(result);
        }
    }
}
