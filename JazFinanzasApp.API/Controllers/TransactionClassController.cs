using JazFinanzasApp.API.Business.DTO.TransactionClass;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{


    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionClassController : ControllerBase
    {
        private readonly ITransactionClassService _transactionClassService;

        public TransactionClassController(ITransactionClassService transactionClassService)
        {
            _transactionClassService = transactionClassService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var result = await _transactionClassService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransactionClass(TransactionClassDTO transactionClassDTO)
        {
            await _transactionClassService.CreateTransactionClassAsync(GetUserId(), transactionClassDTO);
            return Ok(transactionClassDTO);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _transactionClassService.GetByIdAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransactionClass(int id, TransactionClassDTO transactionClassDTO)
        {
            await _transactionClassService.UpdateTransactionClassAsync(GetUserId(), id, transactionClassDTO);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransactionClass(int id)
        {
            await _transactionClassService.DeleteTransactionClassAsync(GetUserId(), id);
            return Ok();
        }
    }
}
