using JazFinanzasApp.API.Business.DTO.Tag;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _tagService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TagAddDTO dto)
        {
            var created = await _tagService.CreateTagAsync(GetUserId(), dto);
            return Ok(created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TagEditDTO dto)
        {
            await _tagService.UpdateTagAsync(GetUserId(), id, dto);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _tagService.DeleteTagAsync(GetUserId(), id);
            return Ok();
        }

        [HttpPost("{tagId}/transaction/{transactionId}")]
        public async Task<IActionResult> AssignToTransaction(int tagId, int transactionId)
        {
            await _tagService.AssignToTransactionAsync(GetUserId(), transactionId, tagId);
            return Ok();
        }

        [HttpDelete("{tagId}/transaction/{transactionId}")]
        public async Task<IActionResult> UnassignFromTransaction(int tagId, int transactionId)
        {
            await _tagService.UnassignFromTransactionAsync(GetUserId(), transactionId, tagId);
            return Ok();
        }

        [HttpPost("{tagId}/cardTransaction/{cardTransactionId}")]
        public async Task<IActionResult> AssignToCardTransaction(int tagId, int cardTransactionId)
        {
            await _tagService.AssignToCardTransactionAsync(GetUserId(), cardTransactionId, tagId);
            return Ok();
        }

        [HttpDelete("{tagId}/cardTransaction/{cardTransactionId}")]
        public async Task<IActionResult> UnassignFromCardTransaction(int tagId, int cardTransactionId)
        {
            await _tagService.UnassignFromCardTransactionAsync(GetUserId(), cardTransactionId, tagId);
            return Ok();
        }

        [HttpGet("transaction/{transactionId}")]
        public async Task<IActionResult> GetForTransaction(int transactionId)
        {
            var result = await _tagService.GetTagsForTransactionAsync(GetUserId(), transactionId);
            return Ok(result);
        }

        [HttpGet("cardTransaction/{cardTransactionId}")]
        public async Task<IActionResult> GetForCardTransaction(int cardTransactionId)
        {
            var result = await _tagService.GetTagsForCardTransactionAsync(GetUserId(), cardTransactionId);
            return Ok(result);
        }
    }
}
