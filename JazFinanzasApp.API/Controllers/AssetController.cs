using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Asset;
using JazFinanzasApp.API.Models.DTO.movementClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAsset_UserRepository _asset_UserRepository;
        private readonly IAssetTypeRepository _assetTypeRepository;
        public AssetController(IAssetRepository assetRepository, IAsset_UserRepository asset_UserRepository, 
            IAssetTypeRepository assetTypeRepository)
        {
            _assetRepository = assetRepository;
            _asset_UserRepository = asset_UserRepository;
            _assetTypeRepository = assetTypeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() 
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var assets = await _assetRepository.GetAssetsAsync();

            var assetsDTO = assets.Select(a => new AssetDTO
            {
                Id = a.Id,
                Name = a.Name,
                AssetTypeName = a.AssetType.Name
            }).ToList();
            return Ok(assetsDTO);
        }

        [HttpGet("type/{assetTypeId}")]
        public async Task<IActionResult> GetAssetsByType(int assetTypeId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var assetType = await _assetTypeRepository.GetByIdAsync(assetTypeId);
            if (assetType == null)
            {
                return NotFound();
            }

            var assets = await _assetRepository.GetAssetsByTypeAsync(assetTypeId);

            var assetsDTO = assets.Select(a => new AssetDTO
            {
                Id = a.Id,
                Name = a.Name,
                AssetTypeName = a.AssetType.Name
            }).ToList();
            return Ok(assetsDTO);
        }

        [HttpGet("user-assets")]
        public async Task<IActionResult> GetUserAssets()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var assets = await _asset_UserRepository.GetUserAssetAsync(userId);

            var assetsDTO = assets.Select(a => new AssetDTO
            {
                Id = a.AssetId,
                Name = a.Asset.Name,
                AssetTypeName = a.Asset.AssetType.Name
            }).ToList();

            return Ok(assetsDTO);
        }

        [HttpPost("assign-assets")]
        public async Task<IActionResult> AssignAsssetsToUser([FromBody] List<int> assetsId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            if (assetsId == null || !assetsId.Any())
            {
                return BadRequest("No assets to assign");
            }

            foreach(var assetId in assetsId)
            {
                await _asset_UserRepository.AssignAssetToUserAsync(int.Parse(userIdClaim.Value), assetId);
            }

            return Ok();
        }

        [HttpDelete("unassign-assets")]
        public async Task<IActionResult> UnassignAssetsToUser([FromBody] List<int> assetsId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            if (assetsId == null || !assetsId.Any())
            {
                return BadRequest("No assets to unassign");
            }

            var failedToUnassign = new List<int>();
            var successToUnassign = new List<int>();

            foreach (var assetId in assetsId)
            {
                // luego agregar chequeo de si el asset se puede borrar
                bool hasMovements = false;
                if(hasMovements)
                {
                    failedToUnassign.Add(assetId);
                }
                else
                {
                    
                    await _asset_UserRepository.UnassignAssetToUserAsync(int.Parse(userIdClaim.Value), assetId);
                    successToUnassign.Add(assetId);
                }               
            }

            if (failedToUnassign.Any())
            {
                return Ok( new
                {
                    message = "Some assets could not be unassigned.",
                    successAssets = successToUnassign,
                    failedAssets = failedToUnassign
                });
            }


            return Ok(new { message = "All assets unassigned successfully." });
        }
 
    }
}
