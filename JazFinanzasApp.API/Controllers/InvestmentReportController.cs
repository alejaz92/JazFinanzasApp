using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InvestmentReportController : ControllerBase
    {
        private readonly IInvestmentReportService _investmentReportService;

        public InvestmentReportController(IInvestmentReportService investmentReportService)
        {
            _investmentReportService = investmentReportService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("Overview/{assetId}")]
        public async Task<IActionResult> GetOverview(int assetId)
        {
            var result = await _investmentReportService.GetOverviewAsync(GetUserId(), assetId);
            return Ok(result);
        }

        [HttpGet("Portfolios/{assetId}")]
        public async Task<IActionResult> GetPortfoliosOverview(int assetId)
        {
            var result = await _investmentReportService.GetPortfoliosOverviewAsync(GetUserId(), assetId);
            return Ok(result);
        }

        [HttpGet("Portfolios/{portfolioId}/Detail/{assetId}")]
        public async Task<IActionResult> GetPortfolioDetail(int portfolioId, int assetId)
        {
            var result = await _investmentReportService.GetPortfolioDetailAsync(GetUserId(), portfolioId, assetId);
            return Ok(result);
        }

        [HttpGet("Stocks/{assetId}")]
        public async Task<IActionResult> GetStocks(int assetId)
        {
            var result = await _investmentReportService.GetStocksAsync(GetUserId(), assetId);
            return Ok(result);
        }

        [HttpGet("Crypto/{assetId}")]
        public async Task<IActionResult> GetCryptoOverview(int assetId, [FromQuery] bool includeStables = true)
        {
            var result = await _investmentReportService.GetCryptoOverviewAsync(GetUserId(), assetId, includeStables);
            return Ok(result);
        }

        [HttpGet("Crypto/{cryptoAssetId}/Detail/{assetId}")]
        public async Task<IActionResult> GetCryptoDetail(int cryptoAssetId, int assetId)
        {
            var result = await _investmentReportService.GetCryptoDetailAsync(GetUserId(), cryptoAssetId, assetId);
            return Ok(result);
        }

        [HttpGet("ContributionsVsPerformance/{assetId}")]
        public async Task<IActionResult> GetContributionsVsPerformance(int assetId)
        {
            var result = await _investmentReportService.GetContributionsVsPerformanceAsync(GetUserId(), assetId);
            return Ok(result);
        }
    }
}
