using JazFinanzasApp.API.Business.DTO.Asset;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _assetService.GetAllAssetsAsync();
            return Ok(result);
        }

        [HttpGet("type")]
        public async Task<IActionResult> GetAssetTypes()
        {
            var result = await _assetService.GetAssetTypesAsync();
            return Ok(result);
        }

        [HttpGet("type/{assetTypeId}")]
        public async Task<IActionResult> GetAssetsByType(int assetTypeId)
        {
            var result = await _assetService.GetAssetsByTypeAsync(assetTypeId);
            return Ok(result);
        }

        [HttpGet("user-assets/{assetTypeId}")]
        public async Task<IActionResult> GetUserAssets(int assetTypeId)
        {
            var result = await _assetService.GetUserAssetsAsync(GetUserId(), assetTypeId);
            return Ok(result);
        }

        [HttpPost("assign-asset")]
        public async Task<IActionResult> AssignAsssetToUser([FromBody] int assetId)
        {
            await _assetService.AssignAssetToUserAsync(GetUserId(), assetId);
            return Ok();
        }

        [HttpPost("unassign-asset")]
        public async Task<IActionResult> UnassignAssetToUser([FromBody] int assetId)
        {
            await _assetService.UnassignAssetFromUserAsync(GetUserId(), assetId);
            return Ok();
        }

        [HttpGet("user-assetsByName/{assetTypeName}")]
        public async Task<IActionResult> GetUserAssetsByATName(string assetTypeName)
        {
            var result = await _assetService.GetUserAssetsByTypeNameAsync(GetUserId(), assetTypeName);
            return Ok(result);
        }

        [HttpGet("card")]
        public async Task<IActionResult> GetAssetsForCardTransactions()
        {
            var result = await _assetService.GetAssetsForCardTransactionsAsync();
            return Ok(result);
        }

        [HttpPut("updateReference")]
        public async Task<IActionResult> UpdateReference([FromBody] AssetDTO assetDTO)
        {
            await _assetService.UpdateReferenceAsync(GetUserId(), assetDTO);
            return Ok();
        }

        [HttpGet("reference")]
        public async Task<IActionResult> GetReferenceAssets()
        {
            var result = await _assetService.GetReferenceAssetsAsync(GetUserId());
            return Ok(result);
        }

        [HttpPut("updateMainReference")]
        public async Task<IActionResult> UpdateMainReference([FromBody] AssetDTO assetDTO)
        {
            await _assetService.UpdateMainReferenceAsync(GetUserId(), assetDTO);
            return Ok();
        }
    }
}
