using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortFolioTransactionController : ControllerBase
    {
        private readonly IInvestmentTransactionService _investmentTransactionService;

        public PortFolioTransactionController(IInvestmentTransactionService investmentTransactionService)
        {
            _investmentTransactionService = investmentTransactionService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetPaginatedPortfolioTransaction(int page = 1, int pageSize = 10, string environment = "PortfolioExchange")
        {
            var (transactions, totalCount) = await _investmentTransactionService.GetPaginatedPortfolioTransactionsAsync(GetUserId(), page, pageSize);
            return Ok(new { transactionsDTO = transactions, totalCount });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePortfolioTransaction([FromBody] PortfolioTransactionAddDTO transactionDTO)
        {
            await _investmentTransactionService.CreatePortfolioTransactionAsync(GetUserId(), transactionDTO);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortfolioTransaction(int id)
        {
            await _investmentTransactionService.DeletePortfolioTransactionAsync(GetUserId(), id);
            return Ok();
        }
    }
}
