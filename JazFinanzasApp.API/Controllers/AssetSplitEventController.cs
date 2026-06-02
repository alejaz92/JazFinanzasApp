using JazFinanzasApp.API.Business.DTO.AssetSplitEvent;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AssetSplitEventController : ControllerBase
    {
        private readonly IAssetSplitEventService _assetSplitEventService;

        public AssetSplitEventController(IAssetSplitEventService assetSplitEventService)
        {
            _assetSplitEventService = assetSplitEventService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetByAssetId([FromQuery] int assetId)
        {
            var splits = await _assetSplitEventService.GetByAssetIdAsync(assetId);
            return Ok(splits);
        }

        [HttpPost]
        public async Task<IActionResult> Add(AssetSplitEventAddDTO dto)
        {
            await _assetSplitEventService.AddAsync(dto, GetUserId());
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _assetSplitEventService.DeleteAsync(id, GetUserId());
            return Ok();
        }
    }
}
