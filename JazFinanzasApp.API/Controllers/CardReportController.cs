using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CardReportController : ControllerBase
    {
        private readonly ICardReportService _cardReportService;

        public CardReportController(ICardReportService cardReportService)
        {
            _cardReportService = cardReportService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("General/{assetId}")]
        public async Task<IActionResult> GetGeneral(int assetId)
        {
            var result = await _cardReportService.GetGeneralAsync(GetUserId(), assetId);
            return Ok(result);
        }

        [HttpGet("ByCard/{cardId}/{assetId}")]
        public async Task<IActionResult> GetByCard(int cardId, int assetId)
        {
            var result = await _cardReportService.GetByCardAsync(GetUserId(), cardId, assetId);
            return Ok(result);
        }

        [HttpGet("FutureCommitment/{assetId}")]
        public async Task<IActionResult> GetFutureCommitment(int assetId)
        {
            var result = await _cardReportService.GetFutureCommitmentAsync(GetUserId(), assetId);
            return Ok(result);
        }

        [HttpGet("Promotions/{assetId}")]
        public async Task<IActionResult> GetPromotions(int assetId)
        {
            var result = await _cardReportService.GetPromotionsAsync(GetUserId(), assetId);
            return Ok(result);
        }

        [HttpGet("MonthSummary")]
        public async Task<IActionResult> GetMonthSummary([FromQuery] DateTime month, [FromQuery] int cardId = 0)
        {
            var result = await _cardReportService.GetMonthSummaryAsync(GetUserId(), month, cardId);
            return Ok(result);
        }
    }
}
