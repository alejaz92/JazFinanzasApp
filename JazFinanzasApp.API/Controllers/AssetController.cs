using JazFinanzasApp.API.Business.DTO.Asset;
using JazFinanzasApp.API.Infrastructure.Domain;
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



            var assets = await _asset_UserRepository.GetUserAssetsAsync(userId, assetTypeId);

            var assetsDTO = assets.Select(a => new AssetDTO
            {
                Id = a.AssetId,
                Name = a.Asset.Name,
                Symbol = a.Asset.Symbol,
                AssetTypeName = a.Asset.AssetType.Name,
                IsReference = a.isReference,
                IsMainReference = a.isMainReference

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

            // check if the asset is used in transactions
            var isUsed = await _asset_UserRepository.IsAssetUserInUseAsync(int.Parse(userIdClaim.Value), assetId);
            if (isUsed)
            {
                return BadRequest("Asset is used in transactions");
            }

            await _asset_UserRepository.UnassignAssetToUserAsync(int.Parse(userIdClaim.Value), assetId);
            return Ok();
            



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


            var assets = await _asset_UserRepository.GetUserAssetsAsync(userId, assetType.Id);

            var assetsDTO = assets.Select(a => new AssetDTO
            {
                Id = a.AssetId,
                Name = a.Asset.Name,
                Symbol = a.Asset.Symbol,
                AssetTypeName = a.Asset.AssetType.Name
            }).ToList();

            return Ok(assetsDTO);
        }

        [HttpGet("card")]
        public async Task<IActionResult> GetAssetsForCardTransactions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            //traer los assets cuyo nombre son Peso Argentino y Dolar Estadounidense con metodo GetAssetByNameAsync
            Asset pesoArgentino = await _assetRepository.GetAssetByNameAsync("Peso Argentino");
            Asset dolarEstadounidense = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");

            if (pesoArgentino == null || dolarEstadounidense == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al buscar los assets");
            }

            var assetsDTO = new List<AssetDTO>
            {
                new AssetDTO
                {
                    Id = pesoArgentino.Id,
                    Name = pesoArgentino.Name,
                    Symbol = pesoArgentino.Symbol,
                    AssetTypeName = pesoArgentino.AssetType.Name
                },
                new AssetDTO
                {
                    Id = dolarEstadounidense.Id,
                    Name = dolarEstadounidense.Name,
                    Symbol = dolarEstadounidense.Symbol,
                    AssetTypeName = dolarEstadounidense.AssetType.Name
                }
            };

            return Ok(assetsDTO);

        }

        [HttpPut("updateReference")]
        public async Task<IActionResult> UpdateReference([FromBody] AssetDTO assetDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            if (assetDTO == null)
            {
                return BadRequest("No asset to update");
            }

            var asset = await _assetRepository.GetByIdAsync(assetDTO.Id);
            if (asset == null)
            {
                return NotFound();
            }

            var user_asset = await _asset_UserRepository.GetUserAssetAsync(int.Parse(userIdClaim.Value), assetDTO.Id);
            if (user_asset == null)
            {
                return NotFound();
            }

            if (assetDTO.IsReference)
            {
                // only allow a max of 3 reference assets
                var referenceAssets = await _asset_UserRepository.GetReferenceAssetsAsync(int.Parse(userIdClaim.Value));
                if (referenceAssets.Count() >= 3)
                {
                    return BadRequest("Only 3 reference assets allowed");
                }
                if (referenceAssets.Count() == 0)
                {
                    user_asset.isMainReference = true;
                }
            } else
            {
                user_asset.isMainReference = false;                
            }

            user_asset.isReference = assetDTO.IsReference;
            await _asset_UserRepository.UpdateAsync(user_asset);

            return Ok();
        }

        //get reference assets
        [HttpGet("reference")]
        public async Task<IActionResult> GetReferenceAssets()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var referenceAssets = await _asset_UserRepository.GetReferenceAssetsAsync(int.Parse(userIdClaim.Value));

            if (referenceAssets.Count() == 0)
            {
                var dollarAsset = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");

                if (dollarAsset == null)
                {
                    return NotFound();
                };
                referenceAssets.Aggregate(new List<Asset_User>(), (list, asset) =>
                {
                    list.Add(new Asset_User
                    {
                        AssetId = dollarAsset.Id,
                        UserId = int.Parse(userIdClaim.Value),
                        isReference = true,
                        isMainReference = true
                    });
                    return list;
                });

            }

            var assetsDTO = referenceAssets.Select(a => new AssetDTO
            {
                Id = a.AssetId,
                Name = a.Asset.Name,
                Symbol = a.Asset.Symbol,
                AssetTypeName = a.Asset.AssetType.Name,
                IsReference = a.isReference,
                IsMainReference = a.isMainReference
            }).ToList();

            return Ok(assetsDTO);
        }

        [HttpPut("updateMainReference")]
        public async Task<IActionResult> UpdateMainReference([FromBody] AssetDTO assetDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            if (assetDTO == null)
            {
                return BadRequest("No asset to update");
            }

            var asset = await _assetRepository.GetByIdAsync(assetDTO.Id);
            if (asset == null)
            {
                return NotFound();
            }

            var user_asset = await _asset_UserRepository.GetUserAssetAsync(int.Parse(userIdClaim.Value), assetDTO.Id);
            if (user_asset == null)
            {
                return NotFound();
            }

            // continue if it is reference
            if (!user_asset.isReference)
            {
                return BadRequest("Asset is not a reference");
            }

            user_asset.isReference = assetDTO.IsReference;
            await _asset_UserRepository.UpdateAsync(user_asset);

            //set the main reference asset
            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(int.Parse(userIdClaim.Value));
            if (mainReferenceAsset != null && assetDTO.IsMainReference && mainReferenceAsset.AssetId != assetDTO.Id)
            {
                mainReferenceAsset.isMainReference = false;
                await _asset_UserRepository.UpdateAsync(mainReferenceAsset);
            }

            user_asset.isMainReference = true;
            await _asset_UserRepository.UpdateAsync(user_asset);

            return Ok();
        }
    }
}
