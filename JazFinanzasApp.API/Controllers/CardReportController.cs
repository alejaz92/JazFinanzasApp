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

        [HttpGet("General")]
        public async Task<IActionResult> GetGeneral()
        {
            var result = await _cardReportService.GetGeneralAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("ByCard/{cardId}")]
        public async Task<IActionResult> GetByCard(int cardId)
        {
            var result = await _cardReportService.GetByCardAsync(GetUserId(), cardId);
            return Ok(result);
        }

        [HttpGet("FutureCommitment")]
        public async Task<IActionResult> GetFutureCommitment()
        {
            var result = await _cardReportService.GetFutureCommitmentAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("Promotions")]
        public async Task<IActionResult> GetPromotions()
        {
            var result = await _cardReportService.GetPromotionsAsync(GetUserId());
            return Ok(result);
        }
    }
}
