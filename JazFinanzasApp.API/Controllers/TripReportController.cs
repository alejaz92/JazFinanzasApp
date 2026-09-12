using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TripReportController : ControllerBase
    {
        private readonly ITripReportService _tripReportService;

        public TripReportController(ITripReportService tripReportService)
        {
            _tripReportService = tripReportService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("General/{assetId}")]
        public async Task<IActionResult> GetGeneral(int assetId)
        {
            var result = await _tripReportService.GetGeneralAsync(GetUserId(), assetId);
            return Ok(result);
        }

        [HttpGet("{tripId}/Detail/{assetId}")]
        public async Task<IActionResult> GetDetail(int tripId, int assetId)
        {
            var result = await _tripReportService.GetDetailAsync(GetUserId(), tripId, assetId);
            return Ok(result);
        }
    }
}
