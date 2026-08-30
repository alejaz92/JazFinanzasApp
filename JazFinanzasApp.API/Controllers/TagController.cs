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
        public async Task<IActionResult> GetAllForUser()
        {
            var result = await _tagService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _tagService.GetByIdAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag(TagDTO tagDTO)
        {
            await _tagService.CreateTagAsync(GetUserId(), tagDTO);
            return Ok(tagDTO);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, TagDTO tagDTO)
        {
            await _tagService.UpdateTagAsync(GetUserId(), id, tagDTO);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            await _tagService.DeleteTagAsync(GetUserId(), id);
            return Ok();
        }

        [HttpGet("transactions/{transactionId}")]
        public async Task<IActionResult> GetTagsForTransaction(int transactionId)
        {
            var result = await _tagService.GetTagsForTransactionAsync(GetUserId(), transactionId);
            return Ok(result);
        }

        [HttpPost("{id}/transactions/{transactionId}")]
        public async Task<IActionResult> AssignToTransaction(int id, int transactionId)
        {
            await _tagService.AssignToTransactionAsync(GetUserId(), id, transactionId);
            return Ok();
        }

        [HttpDelete("{id}/transactions/{transactionId}")]
        public async Task<IActionResult> UnassignFromTransaction(int id, int transactionId)
        {
            await _tagService.UnassignFromTransactionAsync(GetUserId(), id, transactionId);
            return Ok();
        }

        [HttpGet("card-transactions/{cardTransactionId}")]
        public async Task<IActionResult> GetTagsForCardTransaction(int cardTransactionId)
        {
            var result = await _tagService.GetTagsForCardTransactionAsync(GetUserId(), cardTransactionId);
            return Ok(result);
        }

        [HttpPost("{id}/card-transactions/{cardTransactionId}")]
        public async Task<IActionResult> AssignToCardTransaction(int id, int cardTransactionId)
        {
            await _tagService.AssignToCardTransactionAsync(GetUserId(), id, cardTransactionId);
            return Ok();
        }

        [HttpDelete("{id}/card-transactions/{cardTransactionId}")]
        public async Task<IActionResult> UnassignFromCardTransaction(int id, int cardTransactionId)
        {
            await _tagService.UnassignFromCardTransactionAsync(GetUserId(), id, cardTransactionId);
            return Ok();
        }
    }
}
