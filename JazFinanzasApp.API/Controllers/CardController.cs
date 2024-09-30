using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Card;
using JazFinanzasApp.API.Models.DTO.movementClasses;
using JazFinanzasApp.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CardController : ControllerBase
    {
        private readonly ICardRepository _cardRepository;
        public CardController(ICardRepository cardRepository)
        {
            _cardRepository = cardRepository;
        }

        // estoy probando el ui y necesito un metodo add que no requiera autenticar el usuario, que pase por defecto el usuario 1
        [HttpPost("test")]
        public async Task<IActionResult> AddCard(CardDTO cardDTO)
        {
            var card = new Card
            {
                Name = cardDTO.Name,
                UserId = 1
            };

            await _cardRepository.AddAsync(card);
            return Ok();
        }

        // estoy probando el ui y necesito un metodo get que no requiera autenticar el usuario, que pase por defecto el usuario 1
        [HttpGet("test")]
        public async Task<IActionResult> GetCard()
        {
            var cards = await _cardRepository.GetByUserIdAsync(1);

            // convert to DTO

            var cardsDTO = cards.Select(c => new CardDTO
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();
            return Ok(cardsDTO);
        }

        // metodo put sin autenticar
        [HttpPut("test/{id}")]
        public async Task<IActionResult> UpdateCard2(int id, CardDTO cardDTO)
        {
            var card = await _cardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            card.Name = cardDTO.Name;
            card.UpdatedAt = DateTime.UtcNow;

            await _cardRepository.UpdateAsync(card);

            return Ok(cardDTO);
        }

        // metodo delete sin autenticar
        [HttpDelete("test/{id}")]
        public async Task<IActionResult> DeleteCard2(int id)
        {
            var card = await _cardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            await _cardRepository.DeleteAsync(id);

            return Ok();
        }

        // estoy probando el ui y necesito un metodo get by id que no requiera autenticar
        [HttpGet("test/{id}")]
        public async Task<IActionResult> GetCardById(int id)
        {
            var card = await _cardRepository.GetByIdAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            var cardDTO = new CardDTO
            {
                Id = card.Id,
                Name = card.Name
            };

            return Ok(cardDTO);
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

            await _cardRepository.DeleteAsync(id);

            return Ok();
        }
    }
}
