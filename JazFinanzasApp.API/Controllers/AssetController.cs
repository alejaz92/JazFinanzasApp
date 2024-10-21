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
                Symbol = a.Symbol,
                AssetTypeName = a.AssetType.Name
            }).ToList();
            return Ok(assetsDTO);
        }

        [HttpGet("type")]
        public async Task<IActionResult> GetAssetTypes()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var assetTypes = await _assetTypeRepository.GetAllAsync();

            var assetTypesDTO = assetTypes.Select(at => new AssetTypeDTO
            {
                Id = at.Id,
                Name = at.Name
            }).ToList();
            return Ok(assetTypesDTO);
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
                Symbol = a.Symbol,
                AssetTypeName = a.AssetType.Name
            }).ToList();
            return Ok(assetsDTO);
        }

        [HttpGet("user-assets/{assetTypeId}")]
        public async Task<IActionResult> GetUserAssets(int assetTypeId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);



            var assets = await _asset_UserRepository.GetUserAssetAsync(userId, assetTypeId);

            var assetsDTO = assets.Select(a => new AssetDTO
            {
                Id = a.AssetId,
                Name = a.Asset.Name,
                Symbol = a.Asset.Symbol
                //AssetTypeName = a.Asset.AssetType.Name
            }).ToList();

            return Ok(assetsDTO);
        }

        [HttpPost("assign-asset")]
        public async Task<IActionResult> AssignAsssetToUser([FromBody] int assetId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            if (assetId == null)
            {
                return BadRequest("No asset to assign");
            }

            await _asset_UserRepository.AssignAssetToUserAsync(int.Parse(userIdClaim.Value), assetId);
            return Ok();
        }

        [HttpPost("unassign-asset")]
        public async Task<IActionResult> UnassignAssetToUser([FromBody] int assetId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            if (assetId == 0)
            {
                return BadRequest("No asset to unassign");
            }



            // luego agregar chequeo de si el asset se puede borrar
            bool hasMovements = false;
            if(hasMovements)
            {
                return BadRequest("Asset has movements, cannot be unassigned");
            }
            else
            {
                    
                await _asset_UserRepository.UnassignAssetToUserAsync(int.Parse(userIdClaim.Value), assetId);
                return Ok();
            }



        }


        [HttpGet("user-assetsByName/{assetTypeName}")]
        public async Task<IActionResult> GetUserAssetsByATName(string assetTypeName)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            AssetType assetType = await _assetTypeRepository.GetByName(assetTypeName);
            if (assetType == null)
            {
                return NotFound();
            }


            var assets = await _asset_UserRepository.GetUserAssetAsync(userId, assetType.Id);

            var assetsDTO = assets.Select(a => new AssetDTO
            {
                Id = a.AssetId,
                Name = a.Asset.Name,
                Symbol = a.Asset.Symbol,
                AssetTypeName = a.Asset.AssetType.Name
            }).ToList();

            return Ok(assetsDTO);
        }

    }
}
