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
    public class StockTransactionController : ControllerBase
    {
        private readonly IInvestmentTransactionService _investmentTransactionService;

        public StockTransactionController(IInvestmentTransactionService investmentTransactionService)
        {
            _investmentTransactionService = investmentTransactionService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        public async Task<IActionResult> CreateStockTransaction(StockTransactionAddDTO stockTransactionDto)
        {
            await _investmentTransactionService.CreateStockTransactionAsync(GetUserId(), stockTransactionDto);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetPaginatedStockTransactions(int page = 1, int pageSize = 20, string environment = "Stock")
        {
            var (transactions, totalCount) = await _investmentTransactionService.GetPaginatedStockTransactionsAsync(GetUserId(), page, pageSize, environment);
            return Ok(new { transactionsDTO = transactions, totalCount });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockTransaction(int id)
        {
            await _investmentTransactionService.DeleteStockTransactionAsync(GetUserId(), id);
            return Ok();
        }
    }
}
