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
        private readonly ICardPaymentRepository _cardPaymentRepository;

        public CardService(ICardRepository cardRepository, ICardPaymentRepository cardPaymentRepository)
        {
            _cardRepository = cardRepository;
            _cardPaymentRepository = cardPaymentRepository;
        }

        public async Task<IEnumerable<CardDTO>> GetAllForUserAsync(int userId)
        {
            var cards = await _cardRepository.GetByUserIdAsync(userId);
            var dtos = new List<CardDTO>();
            foreach (var c in cards)
            {
                dtos.Add(new CardDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    NextClosingDate = c.NextClosingDate,
                    NextDueDate = c.NextDueDate,
                    IsCurrentPeriodPaid = await IsCurrentPeriodPaidAsync(c)
                });
            }
            return dtos;
        }

        public async Task<CardDTO> GetByIdAsync(int userId, int id)
        {
            var card = await _cardRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card not found");
            if (card.UserId != userId) throw new UnauthorizedDomainException();
            return new CardDTO
            {
                Id = card.Id,
                Name = card.Name,
                NextClosingDate = card.NextClosingDate,
                NextDueDate = card.NextDueDate,
                IsCurrentPeriodPaid = await IsCurrentPeriodPaidAsync(card)
            };
        }

        private async Task<bool> IsCurrentPeriodPaidAsync(Card card)
        {
            if (!card.NextClosingDate.HasValue) return false;
            return await _cardPaymentRepository.IsPaymentAlreadyMadeAsync(card.Id, card.NextClosingDate.Value);
        }

        public async Task CreateCardAsync(int userId, CardDTO dto)
        {
            var checkExists = await _cardRepository.FindAsync(c => c.Name == dto.Name && c.UserId == userId);
            if (checkExists.Any()) throw new BusinessRuleException("Card already exists");
            ValidateClosingDates(dto);
            await _cardRepository.AddAsync(new Card
            {
                Name = dto.Name,
                UserId = userId,
                NextClosingDate = dto.NextClosingDate,
                NextDueDate = dto.NextDueDate
            });
        }

        public async Task UpdateCardAsync(int userId, int id, CardDTO dto)
        {
            var card = await _cardRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card not found");
            if (card.UserId != userId) throw new UnauthorizedDomainException();
            ValidateClosingDates(dto);
            card.Name = dto.Name;
            card.NextClosingDate = dto.NextClosingDate;
            card.NextDueDate = dto.NextDueDate;
            card.UpdatedAt = DateTime.UtcNow;
            await _cardRepository.UpdateAsync(card);
        }

        private static void ValidateClosingDates(CardDTO dto)
        {
            if (dto.NextClosingDate.HasValue && dto.NextDueDate.HasValue
                && dto.NextDueDate.Value < dto.NextClosingDate.Value)
            {
                throw new BusinessRuleException("NextDueDate must be on or after NextClosingDate");
            }
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
