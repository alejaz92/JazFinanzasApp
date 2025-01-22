using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Card;
using JazFinanzasApp.API.Models.DTO.transactionClass;
using JazFinanzasApp.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CardController : ControllerBase
    {
        private readonly ICardRepository _cardRepository;
        public CardController(ICardRepository cardRepository)
        {
            _cardRepository = cardRepository;
        }          

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var cards = await _cardRepository.GetByUserIdAsync(userId);

            // convert to DTO

            var cardsDTO = cards.Select(c => new CardDTO
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();
            return Ok(cardsDTO);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCard(CardDTO cardDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var checkExists = await _cardRepository.FindAsync(c => c.Name == cardDTO.Name && c.UserId == userId);
            if(checkExists.Any())
            {
                return BadRequest("Card already exists");
            }

            var card = new Card
            {
                Name = cardDTO.Name,
                UserId = userId
            };

            await _cardRepository.AddAsync(card);
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var card = await _cardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            if (card.UserId != userId)
            {
                return Unauthorized();
            }

            var cardDTO = new CardDTO
            {
                Name = card.Name
            };

            return Ok(cardDTO);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCard(int id, CardDTO cardDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var card = await _cardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            if (card.UserId != userId)
            {
                return Unauthorized();
            }

            card.Name = cardDTO.Name;
            card.UpdatedAt = DateTime.UtcNow;

            await _cardRepository.UpdateAsync(card);

            return Ok(cardDTO);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var card = await _cardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            if (card.UserId != userId)
            {
                return Unauthorized();
            }

            if ( await _cardRepository.IsCardUsed(id))
            {
                return BadRequest("Card is used in transactions");
            }

            await _cardRepository.DeleteAsync(id);

            return Ok();
        }

    }
}
