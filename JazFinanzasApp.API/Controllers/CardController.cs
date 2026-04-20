using JazFinanzasApp.API.Business.DTO.Card;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CardController : ControllerBase
    {
        private readonly ICardService _cardService;

        public CardController(ICardService cardService)
        {
            _cardService = cardService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var result = await _cardService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCard(CardDTO cardDTO)
        {
            await _cardService.CreateCardAsync(GetUserId(), cardDTO);
            return Ok(cardDTO);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _cardService.GetByIdAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCard(int id, CardDTO cardDTO)
        {
            await _cardService.UpdateCardAsync(GetUserId(), id, cardDTO);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            await _cardService.DeleteCardAsync(GetUserId(), id);
            return Ok();
        }
    }
}
