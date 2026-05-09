using JazFinanzasApp.API.Business.DTO.Transaction;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetPaginatedTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (transactions, totalCount) = await _transactionService.GetPaginatedTransactionsAsync(GetUserId(), page, pageSize);
            return Ok(new { Transactions = transactions, TotalCount = totalCount });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction(TransactionAddDTO transactionDTO)
        {
            await _transactionService.CreateTransactionAsync(GetUserId(), transactionDTO);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditTransaction(int id, TransactionEditDTO transactionDTO)
        {
            await _transactionService.EditTransactionAsync(GetUserId(), id, transactionDTO);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            await _transactionService.DeleteTransactionAsync(GetUserId(), id);
            return Ok();
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetTransaction(int Id)
        {
            var result = await _transactionService.GetTransactionByIdAsync(GetUserId(), Id);
            return Ok(result);
        }

        [HttpPost("refund/{Id}")]
        public async Task<IActionResult> RefundTransaction(int Id, [FromBody] RefundDTO refundDTO)
        {
            await _transactionService.RefundTransactionAsync(GetUserId(), Id, refundDTO);
            return Ok();
        }

        [HttpGet("exchange")]
        public async Task<IActionResult> GetPaginatedExchangeTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (transactions, totalCount) = await _transactionService.GetPaginatedExchangeTransactionsAsync(GetUserId(), page, pageSize);
            return Ok(new { Transactions = transactions, TotalCount = totalCount });
        }

        [HttpDelete("exchange/{id}")]
        public async Task<IActionResult> DeleteExchangeTransaction(int id)
        {
            await _transactionService.DeleteExchangeTransactionAsync(GetUserId(), id);
            return Ok();
        }
    }       
}
