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
        public async Task<IActionResult> GetPortfoliosOverview(int assetId, [FromQuery] bool includeCash = true)
        {
            var result = await _investmentReportService.GetPortfoliosOverviewAsync(GetUserId(), assetId, includeCash);
            return Ok(result);
        }

        [HttpGet("Portfolios/{portfolioId}/Detail/{assetId}")]
        public async Task<IActionResult> GetPortfolioDetail(int portfolioId, int assetId, [FromQuery] bool includeCash = true)
        {
            var result = await _investmentReportService.GetPortfolioDetailAsync(GetUserId(), portfolioId, assetId, includeCash);
            return Ok(result);
        }

        // Bolsa, revisada (Fase 20a): assetTypeId en 0 (default) trae todo el entorno (D-11);
        // includeClosed suma las posiciones ya vendidas del todo, apagado por default (D-14).
        [HttpGet("Stocks/{assetId}")]
        public async Task<IActionResult> GetStocks(int assetId, [FromQuery] int assetTypeId = 0, [FromQuery] bool includeClosed = false)
        {
            var result = await _investmentReportService.GetStocksAsync(GetUserId(), assetId, assetTypeId, includeClosed);
            return Ok(result);
        }

        [HttpGet("Crypto/{assetId}")]
        public async Task<IActionResult> GetCryptoOverview(int assetId, [FromQuery] bool includeStables = true)
        {
            var result = await _investmentReportService.GetCryptoOverviewAsync(GetUserId(), assetId, includeStables);
            return Ok(result);
        }

        // Se mantiene con su ruta y su contrato (T14) mientras Cryptos — Detalle (frontend) no
        // consuma la ruta genérica de abajo — las dos llaman al mismo cálculo (T17).
        [HttpGet("Crypto/{cryptoAssetId}/Detail/{assetId}")]
        public async Task<IActionResult> GetCryptoDetail(int cryptoAssetId, int assetId)
        {
            var result = await _investmentReportService.GetAssetDetailAsync(GetUserId(), cryptoAssetId, assetId);
            return Ok(result);
        }

        // Detalle de un activo (T17, D-15): generaliza la ruta de arriba para que Bolsa — Detalle la
        // use también, sin duplicar el cálculo. `assetId` es el activo a detallar, `referenceAssetId`
        // la moneda de referencia de la barra de Reportes.
        [HttpGet("Asset/{assetId}/Detail/{referenceAssetId}")]
        public async Task<IActionResult> GetAssetDetail(int assetId, int referenceAssetId)
        {
            var result = await _investmentReportService.GetAssetDetailAsync(GetUserId(), assetId, referenceAssetId);
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
