using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CryptoTransactionController : ControllerBase
    {
        private readonly IInvestmentTransactionService _investmentTransactionService;

        public CryptoTransactionController(IInvestmentTransactionService investmentTransactionService)
        {
            _investmentTransactionService = investmentTransactionService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        public async Task<IActionResult> CreateCryptoTransaction(InvestmentTransactionAddDTO cryptoTransactionDTO)
        {
            await _investmentTransactionService.CreateCryptoTransactionAsync(GetUserId(), cryptoTransactionDTO);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetPaginatedCryptoTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (transactions, totalCount) = await _investmentTransactionService.GetPaginatedCryptoTransactionsAsync(GetUserId(), page, pageSize);
            return Ok(new { transactions, totalCount });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCryptoTransaction(int id)
        {
            await _investmentTransactionService.DeleteCryptoTransactionAsync(GetUserId(), id);
            return Ok();
        }
    }
}

