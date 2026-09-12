using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SharedEventReportController : ControllerBase
    {
        private readonly ISharedEventReportService _sharedEventReportService;

        public SharedEventReportController(ISharedEventReportService sharedEventReportService)
        {
            _sharedEventReportService = sharedEventReportService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("General")]
        public async Task<IActionResult> GetGeneral()
        {
            var result = await _sharedEventReportService.GetGeneralAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("Person/{personId}")]
        public async Task<IActionResult> GetByPerson(int personId)
        {
            var result = await _sharedEventReportService.GetByPersonAsync(GetUserId(), personId);
            return Ok(result);
        }
    }
}
