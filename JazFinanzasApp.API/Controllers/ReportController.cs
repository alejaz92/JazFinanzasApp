using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("Balance")]
        public async Task<IActionResult> GetTotalsBalance()
        {
            var result = await _reportService.GetTotalsBalanceAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("Balance/{id}")]
        public async Task<IActionResult> GetBalance(int id)
        {
            var result = await _reportService.GetBalanceByAssetAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpGet("IncExpStats")]
        public async Task<IActionResult> GetIncExpStats([FromQuery] DateTime month, [FromQuery] int assetId)
        {
            var result = await _reportService.GetIncExpStatsAsync(GetUserId(), month, assetId);
            return Ok(result);
        }

        [HttpGet("CardStats/{id}")]
        public async Task<IActionResult> GetCardStats(int id)
        {
            var result = await _reportService.GetCardStatsAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpGet("StockStats/{id}")]
        public async Task<IActionResult> GetStockStats(int id)
        {
            var result = await _reportService.GetStockStatsAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpGet("CryptoGralStats")]
        public async Task<IActionResult> GetCryptoGralStats([FromQuery] bool includeStables)
        {
            var result = await _reportService.GetCryptoGralStatsAsync(GetUserId(), includeStables);
            return Ok(result);
        }

        [HttpGet("CryptoStats/{id}")]
        public async Task<IActionResult> GetCryptoStats(int id)
        {
            var result = await _reportService.GetCryptoStatsAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpGet("HomeStats")]
        public async Task<IActionResult> GetHomeStats()
        {
            var result = await _reportService.GetHomeStatsAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("PortfolioStats")]
        public async Task<IActionResult> GetPortfolioStats()
        {
            var result = await _reportService.GetPortfolioStatsAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("PortfolioStats/{portfolioId}")]
        public async Task<IActionResult> GetPortfolioDetailStats(int portfolioId)
        {
            var result = await _reportService.GetPortfolioDetailStatsAsync(GetUserId(), portfolioId);
            return Ok(result);
        }

        [HttpGet("PortfolioStats/{portfolioId}/history")]
        public async Task<IActionResult> GetPortfolioValueHistory(int portfolioId)
        {
            var result = await _reportService.GetPortfolioValueHistoryAsync(GetUserId(), portfolioId);
            return Ok(result);
        }

        [HttpGet("trips-general")]
        public async Task<IActionResult> GetTripsGeneralStats()
        {
            var result = await _reportService.GetTripsGeneralStatsAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("trip-detail/{tripId}")]
        public async Task<IActionResult> GetTripDetailStats(int tripId)
        {
            var result = await _reportService.GetTripDetailStatsAsync(GetUserId(), tripId);
            return Ok(result);
        }
    }
}
