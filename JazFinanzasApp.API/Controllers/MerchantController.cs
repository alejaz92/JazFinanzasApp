using JazFinanzasApp.API.Business.DTO.Merchant;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MerchantController : ControllerBase
    {
        private readonly IMerchantService _merchantService;

        public MerchantController(IMerchantService merchantService)
        {
            _merchantService = merchantService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _merchantService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Rename(int id, MerchantRenameDTO dto)
        {
            await _merchantService.RenameMerchantAsync(GetUserId(), id, dto);
            return Ok();
        }

        [HttpPost("{sourceMerchantId}/merge/{targetMerchantId}")]
        public async Task<IActionResult> Merge(int sourceMerchantId, int targetMerchantId)
        {
            await _merchantService.MergeMerchantsAsync(GetUserId(), sourceMerchantId, targetMerchantId);
            return Ok();
        }

        [HttpPost("{merchantId}/transaction/{transactionId}")]
        public async Task<IActionResult> ReassignTransaction(int merchantId, int transactionId)
        {
            await _merchantService.ReassignTransactionAsync(GetUserId(), transactionId, merchantId);
            return Ok();
        }

        [HttpPost("{merchantId}/cardTransaction/{cardTransactionId}")]
        public async Task<IActionResult> ReassignCardTransaction(int merchantId, int cardTransactionId)
        {
            await _merchantService.ReassignCardTransactionAsync(GetUserId(), cardTransactionId, merchantId);
            return Ok();
        }

        [HttpPost("resolve-all")]
        public async Task<IActionResult> ResolveAll()
        {
            var result = await _merchantService.ResolveAllAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("{id}/movements")]
        public async Task<IActionResult> GetMovements(int id)
        {
            var result = await _merchantService.GetMovementsAsync(GetUserId(), id);
            return Ok(result);
        }
    }
}
