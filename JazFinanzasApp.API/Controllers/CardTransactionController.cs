using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CardTransactionController : ControllerBase
    {
        private readonly ICardTransactionService _cardTransactionService;

        public CardTransactionController(ICardTransactionService cardTransactionService)
        {
            _cardTransactionService = cardTransactionService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        public async Task<IActionResult> AddCardTransaction([FromBody] CardTransactionAddDTO cardTransactionAddDTO)
        {
            var id = await _cardTransactionService.AddCardTransactionAsync(GetUserId(), cardTransactionAddDTO);
            return Ok(new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingCardTransactions()
        {
            var result = await _cardTransactionService.GetPendingCardTransactionsAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("CardPayments")]
        public async Task<IActionResult> GetCardPayments(int CardId, DateTime paymentMonth)
        {
            var result = await _cardTransactionService.GetCardPaymentsAsync(GetUserId(), CardId, paymentMonth);
            return Ok(result);
        }

        [HttpPost("CardPayments")]
        public async Task<IActionResult> RegisterCardPayment([FromBody] CardTransactionPaymentDTO cardTransactionsPaymentDTO)
        {
            await _cardTransactionService.RegisterCardPaymentAsync(GetUserId(), cardTransactionsPaymentDTO);
            return Ok(cardTransactionsPaymentDTO);
        }

        [HttpGet("EditRecurrent/{Id}")]
        public async Task<IActionResult> GetRecurrent(int Id)
        {
            var result = await _cardTransactionService.GetRecurrentTransactionAsync(GetUserId(), Id);
            return Ok(result);
        }

        [HttpPut("EditRecurrent/{Id}")]
        public async Task<IActionResult> EditRecurrent([FromBody] EditRecurrentDTO editRecurrentDTO, int Id)
        {
            await _cardTransactionService.UpdateRecurrentTransactionAsync(GetUserId(), Id, editRecurrentDTO);
            return Ok();
        }
    }     
}

