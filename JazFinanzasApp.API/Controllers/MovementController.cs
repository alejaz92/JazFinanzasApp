using JazFinanzasApp.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MovementController : ControllerBase
    {
        private readonly IMovementRepository _movementRepository;
        public MovementController(IMovementRepository movementRepository)
        {
            _movementRepository = movementRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaginatedMovements([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);
            var (movements, totalCount) = await _movementRepository.GetPaginatedMovements(userId, page, pageSize);

            var movementsDTO = movements.Select(m => new MovementDTO
            {
                Id = m.Id,
                Date = m.Date,
                Amount = m.Amount,
                Detail = m.Detail,
                AccountId = m.AccountId,
                AccountName = m.Account.Name,
                AssetId = m.AssetId,
                AssetName = m.Asset.Name,
                MovementClassId = m.MovementClassId,
                MovementClassName = m.MovementClass.Description,
                MovementType = m.MovementType

            }).ToList();

            return Ok(new { Movements = movementsDTO, TotalCount = totalCount });
        }
       
    }
}
