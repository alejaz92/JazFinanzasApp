using JazFinanzasApp.API.Business.DTO.AssetType;
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
    public class AssetTypeController : ControllerBase
    {
        private readonly IAssetTypeRepository _assetTypeRepository;

        public AssetTypeController(IAssetTypeRepository assetTypeRepository)
        {
            _assetTypeRepository = assetTypeRepository;
        }

       // get method by environmnet parameter
       [HttpGet]
       public async Task<IActionResult> GetAssetTypes([FromQuery] string environment)
         {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var assetTypes = await _assetTypeRepository.GetAssetTypes(environment);

            // adapt to dto
            var assetTypesDTO = assetTypes.Select(a => new AssetTypeDTO
            {
                Id = a.Id,
                Name = a.Name,
                Environment = a.Environment
            });


            return Ok(assetTypesDTO);
         }
    }
}
