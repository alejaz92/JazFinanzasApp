using JazFinanzasApp.API.Business.DTO.SharedExpense;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/shared-expense")]
    [ApiController]
    public class SharedExpenseController : ControllerBase
    {
        private readonly ISharedExpenseService _sharedExpenseService;

        public SharedExpenseController(ISharedExpenseService sharedExpenseService)
        {
            _sharedExpenseService = sharedExpenseService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        public async Task<IActionResult> Create(SharedExpenseAddDTO dto)
        {
            var result = await _sharedExpenseService.CreateAsync(GetUserId(), dto);
            return Ok(result);
        }

        [HttpPost("card")]
        public async Task<IActionResult> CreateForCard(SharedExpenseAddDTO dto)
        {
            var result = await _sharedExpenseService.CreateAsync(GetUserId(), dto);
            return Ok(result);
        }

        [HttpGet("transaction/{transactionId}")]
        public async Task<IActionResult> GetByTransaction(int transactionId)
        {
            var result = await _sharedExpenseService.GetByTransactionIdAsync(GetUserId(), transactionId);
            return Ok(result);
        }

        [HttpGet("card-transaction/{cardTransactionId}")]
        public async Task<IActionResult> GetByCardTransaction(int cardTransactionId)
        {
            var result = await _sharedExpenseService.GetByCardTransactionIdAsync(GetUserId(), cardTransactionId);
            return Ok(result);
        }

        [HttpPost("reimbursement")]
        public async Task<IActionResult> RegisterReimbursement(RegisterReimbursementDTO dto)
        {
            var result = await _sharedExpenseService.RegisterReimbursementAsync(GetUserId(), dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _sharedExpenseService.DeleteAsync(GetUserId(), id);
            return Ok();
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _sharedExpenseService.GetSummaryAsync(GetUserId());
            return Ok(result);
        }
    }
}
