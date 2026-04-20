using JazFinanzasApp.API.Business.DTO.Card;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class CardService : ICardService
    {
        private readonly ICardRepository _cardRepository;

        public CardService(ICardRepository cardRepository)
        {
            _cardRepository = cardRepository;
        }

        public async Task<IEnumerable<CardDTO>> GetAllForUserAsync(int userId)
        {
            var cards = await _cardRepository.GetByUserIdAsync(userId);
            return cards.Select(c => new CardDTO { Id = c.Id, Name = c.Name });
        }

        public async Task<CardDTO> GetByIdAsync(int userId, int id)
        {
            var card = await _cardRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card not found");
            if (card.UserId != userId) throw new UnauthorizedDomainException();
            return new CardDTO { Id = card.Id, Name = card.Name };
        }

        public async Task CreateCardAsync(int userId, CardDTO dto)
        {
            var checkExists = await _cardRepository.FindAsync(c => c.Name == dto.Name && c.UserId == userId);
            if (checkExists.Any()) throw new BusinessRuleException("Card already exists");
            await _cardRepository.AddAsync(new Card { Name = dto.Name, UserId = userId });
        }

        public async Task UpdateCardAsync(int userId, int id, CardDTO dto)
        {
            var card = await _cardRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card not found");
            if (card.UserId != userId) throw new UnauthorizedDomainException();
            card.Name = dto.Name;
            card.UpdatedAt = DateTime.UtcNow;
            await _cardRepository.UpdateAsync(card);
        }

        public async Task DeleteCardAsync(int userId, int id)
        {
            var card = await _cardRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card not found");
            if (card.UserId != userId) throw new UnauthorizedDomainException();
            await _cardRepository.DeleteAsync(id);
        }
    }
}
