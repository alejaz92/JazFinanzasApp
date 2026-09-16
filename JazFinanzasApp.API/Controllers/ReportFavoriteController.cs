using JazFinanzasApp.API.Business.DTO.ReportFavorite;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportFavoriteController : ControllerBase
    {
        private readonly IReportFavoriteService _reportFavoriteService;

        public ReportFavoriteController(IReportFavoriteService reportFavoriteService)
        {
            _reportFavoriteService = reportFavoriteService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var result = await _reportFavoriteService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateReportFavoriteDTO dto)
        {
            var result = await _reportFavoriteService.CreateAsync(GetUserId(), dto.ReportKey);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _reportFavoriteService.DeleteAsync(GetUserId(), id);
            return Ok();
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder([FromBody] List<int> orderedIds)
        {
            await _reportFavoriteService.ReorderAsync(GetUserId(), orderedIds);
            return Ok();
        }
    }
}
