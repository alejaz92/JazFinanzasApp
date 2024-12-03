using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAsset_UserRepository _asset_UserRepository;

        public ReportController(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAsset_UserRepository asset_UserRepository
            )
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _asset_UserRepository = asset_UserRepository;
        }

        [HttpGet("Balance")]
        public async Task<IActionResult> GetTotalsBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);


            // Get the balance for the user by account

            var balanceDTO = await _transactionRepository.GetTotalsBalanceByUserAsync(userId);
            return Ok(balanceDTO);
        }

        [HttpGet("Balance/{id}")]
        public async Task<IActionResult> GetBalance(int id)
        {
            

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var asset = await _assetRepository.GetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }


            var balanceDTO = await _transactionRepository.GetBalanceByAssetAndUserAsync(id, userId);
            // Get the balance for the user and assetId by account


            return Ok(balanceDTO);
        }
    }
}
