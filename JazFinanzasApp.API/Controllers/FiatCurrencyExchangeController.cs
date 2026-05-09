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
    public class FiatCurrencyExchangeController : ControllerBase
    {
        private readonly IFiatCurrencyExchangeService _fiatCurrencyExchangeService;

        public FiatCurrencyExchangeController(IFiatCurrencyExchangeService fiatCurrencyExchangeService)
        {
            _fiatCurrencyExchangeService = fiatCurrencyExchangeService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetPaginatedExchangeTransactions(int page = 1, int pageSize = 10)
        {
            var (transactions, totalCount) = await _fiatCurrencyExchangeService.GetPaginatedAsync(GetUserId(), page, pageSize);
            return Ok(new { transactionsDTO = transactions, totalCount });
        }

        [HttpPost]
        public async Task<IActionResult> CreateExchangeTransaction(CurrencyExchangeAddDTO exchangeTransactionDTO)
        {
            await _fiatCurrencyExchangeService.CreateExchangeTransactionAsync(GetUserId(), exchangeTransactionDTO);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExchangeTransaction(int id)
        {
            await _fiatCurrencyExchangeService.DeleteExchangeTransactionAsync(GetUserId(), id);
            return Ok();
        }
    }
}
