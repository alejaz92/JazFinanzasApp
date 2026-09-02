using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NetWorthController : ControllerBase
    {
        private readonly INetWorthReportService _netWorthReportService;

        public NetWorthController(INetWorthReportService netWorthReportService)
        {
            _netWorthReportService = netWorthReportService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("General")]
        public async Task<IActionResult> GetGeneral()
        {
            var result = await _netWorthReportService.GetGeneralAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("Monthly/{assetId}")]
        public async Task<IActionResult> GetMonthlySeries(int assetId)
        {
            var result = await _netWorthReportService.GetMonthlySeriesAsync(GetUserId(), assetId);
            return Ok(result);
        }

        [HttpGet("ByAccount/{assetId}")]
        public async Task<IActionResult> GetByAccount(int assetId)
        {
            var result = await _netWorthReportService.GetByAccountAsync(GetUserId(), assetId);
            return Ok(result);
        }

    }
}
