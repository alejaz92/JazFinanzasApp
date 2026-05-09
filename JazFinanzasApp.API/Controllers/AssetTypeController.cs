using JazFinanzasApp.API.Business.DTO.AssetType;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AssetTypeController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetTypeController(IAssetService assetService)
        {
            _assetService = assetService;
        }

       // get method by environment parameter
       [HttpGet]
       public async Task<IActionResult> GetAssetTypes([FromQuery] string environment)
         {
            var result = await _assetService.GetAssetTypesByEnvironmentAsync(environment);
            return Ok(result);
         }
    }
}
