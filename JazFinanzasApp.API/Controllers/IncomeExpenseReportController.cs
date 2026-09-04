using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class IncomeExpenseReportController : ControllerBase
    {
        private readonly IIncomeExpenseReportService _incomeExpenseReportService;

        public IncomeExpenseReportController(IIncomeExpenseReportService incomeExpenseReportService)
        {
            _incomeExpenseReportService = incomeExpenseReportService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("Waterfall/{assetId}")]
        public async Task<IActionResult> GetWaterfall(int assetId, [FromQuery] DateTime month)
        {
            var result = await _incomeExpenseReportService.GetWaterfallAsync(GetUserId(), month, assetId);
            return Ok(result);
        }

        [HttpGet("Evolution/{assetId}")]
        public async Task<IActionResult> GetEvolution(int assetId, [FromQuery] int months = 24)
        {
            var result = await _incomeExpenseReportService.GetEvolutionAsync(GetUserId(), assetId, months);
            return Ok(result);
        }

        [HttpGet("ByCategory/{assetId}")]
        public async Task<IActionResult> GetByCategory(int assetId, [FromQuery] DateTime month)
        {
            var result = await _incomeExpenseReportService.GetByCategoryAsync(GetUserId(), month, assetId);
            return Ok(result);
        }

        [HttpGet("ByTag/{assetId}")]
        public async Task<IActionResult> GetByTag(int assetId, [FromQuery] int months = 6)
        {
            var result = await _incomeExpenseReportService.GetByTagAsync(GetUserId(), assetId, months);
            return Ok(result);
        }

        [HttpGet("Calendar/{assetId}")]
        public async Task<IActionResult> GetCalendar(int assetId, [FromQuery] int year)
        {
            var result = await _incomeExpenseReportService.GetCalendarAsync(GetUserId(), assetId, year);
            return Ok(result);
        }

        [HttpGet("IncomeComposition/{assetId}")]
        public async Task<IActionResult> GetIncomeComposition(int assetId, [FromQuery] DateTime month)
        {
            var result = await _incomeExpenseReportService.GetIncomeCompositionAsync(GetUserId(), month, assetId);
            return Ok(result);
        }

        [HttpGet("IncomeByCategory/{assetId}")]
        public async Task<IActionResult> GetIncomeByCategory(int assetId, [FromQuery] int months = 24)
        {
            var result = await _incomeExpenseReportService.GetIncomeByCategoryAsync(GetUserId(), assetId, months);
            return Ok(result);
        }

        [HttpGet("IncomeByCategoryAndDay/{assetId}")]
        public async Task<IActionResult> GetIncomeByCategoryAndDay(int assetId, [FromQuery] int months = 12)
        {
            var result = await _incomeExpenseReportService.GetIncomeByCategoryAndDayAsync(GetUserId(), assetId, months);
            return Ok(result);
        }
    }
}
