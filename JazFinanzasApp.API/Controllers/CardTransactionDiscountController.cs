using JazFinanzasApp.API.Business.DTO.CardTransactionDiscount;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/card-transaction-discount")]
    [ApiController]
    public class CardTransactionDiscountController : ControllerBase
    {
        private readonly ICardTransactionDiscountService _cardTransactionDiscountService;

        public CardTransactionDiscountController(ICardTransactionDiscountService cardTransactionDiscountService)
        {
            _cardTransactionDiscountService = cardTransactionDiscountService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        public async Task<IActionResult> Create(CardTransactionDiscountAddDTO dto)
        {
            var result = await _cardTransactionDiscountService.CreateAsync(GetUserId(), dto);
            return Ok(result);
        }

        [HttpGet("card-transaction/{cardTransactionId}")]
        public async Task<IActionResult> GetByCardTransaction(int cardTransactionId)
        {
            var result = await _cardTransactionDiscountService.GetByCardTransactionIdAsync(GetUserId(), cardTransactionId);
            return Ok(result);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var result = await _cardTransactionDiscountService.GetActiveByUserIdAsync(GetUserId());
            return Ok(result);
        }

        [HttpPost("{id}/rescue")]
        public async Task<IActionResult> Rescue(int id, CardTransactionDiscountRescueDTO dto)
        {
            var result = await _cardTransactionDiscountService.RescueAsync(GetUserId(), id, dto);
            return Ok(result);
        }

        [HttpGet("card/{cardId}/pending-credit")]
        public async Task<IActionResult> GetPendingCredit(int cardId)
        {
            var result = await _cardTransactionDiscountService.GetPendingOnCardAsync(GetUserId(), cardId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _cardTransactionDiscountService.DeleteAsync(GetUserId(), id);
            return Ok();
        }
    }
}
