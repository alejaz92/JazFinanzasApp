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
        public AssetController(IAssetRepository assetRepository)
        {
            _assetRepository = assetRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var assetsDTO = await _assetRepository.GetAllAssetsAsync();

            // convert to DTO

            //var assetsDTO = assets.Select(a => new AssetDTO
            //{
            //    Name = a.Name,
            //    Symbol = a.Symbol,
            //    AssetTypeName = "algo"
            //}).ToList();
            return Ok(assetsDTO);
        }

    }
}
