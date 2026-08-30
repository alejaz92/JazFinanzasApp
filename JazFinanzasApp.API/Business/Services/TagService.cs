using JazFinanzasApp.API.Business.DTO.Tag;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;

        public TagService(
            ITagRepository tagRepository,
            ITransactionRepository transactionRepository,
            ICardTransactionRepository cardTransactionRepository)
        {
            _tagRepository = tagRepository;
            _transactionRepository = transactionRepository;
            _cardTransactionRepository = cardTransactionRepository;
        }

        public async Task<IEnumerable<TagDTO>> GetAllForUserAsync(int userId)
        {
            var tags = await _tagRepository.GetByUserIdAsync(userId);
            return tags.OrderBy(t => t.Name).Select(ToDto);
        }

        public async Task<TagDTO> GetByIdAsync(int userId, int id)
        {
            var tag = await GetOwnedTagAsync(userId, id);
            return ToDto(tag);
        }

        public async Task CreateTagAsync(int userId, TagDTO dto)
        {
            var existing = await _tagRepository.GetByNameAsync(dto.Name, userId);
            if (existing != null) throw new BusinessRuleException("Tag already exists");

            await _tagRepository.AddAsync(new Tag
            {
                Name = dto.Name,
                Color = dto.Color,
                UserId = userId
            });
        }

        public async Task UpdateTagAsync(int userId, int id, TagDTO dto)
        {
            var tag = await GetOwnedTagAsync(userId, id);

            var existing = await _tagRepository.GetByNameAsync(dto.Name, userId);
            if (existing != null && existing.Id != id) throw new BusinessRuleException("Tag already exists");

            tag.Name = dto.Name;
            tag.Color = dto.Color;
            tag.UpdatedAt = DateTime.UtcNow;
            await _tagRepository.UpdateAsync(tag);
        }

        public async Task DeleteTagAsync(int userId, int id)
        {
            await GetOwnedTagAsync(userId, id);
            await _tagRepository.DeleteAsync(id);
        }

        public async Task AssignToTransactionAsync(int userId, int tagId, int transactionId)
        {
            await GetOwnedTagAsync(userId, tagId);
            await GetOwnedTransactionAsync(userId, transactionId);
            await _tagRepository.AssignToTransactionAsync(tagId, transactionId);
        }

        public async Task UnassignFromTransactionAsync(int userId, int tagId, int transactionId)
        {
            await GetOwnedTagAsync(userId, tagId);
            await GetOwnedTransactionAsync(userId, transactionId);
            await _tagRepository.UnassignFromTransactionAsync(tagId, transactionId);
        }

        public async Task<IEnumerable<TagDTO>> GetTagsForTransactionAsync(int userId, int transactionId)
        {
            await GetOwnedTransactionAsync(userId, transactionId);
            var tags = await _tagRepository.GetTagsForTransactionAsync(transactionId);
            return tags.OrderBy(t => t.Name).Select(ToDto);
        }

        public async Task AssignToCardTransactionAsync(int userId, int tagId, int cardTransactionId)
        {
            await GetOwnedTagAsync(userId, tagId);
            await GetOwnedCardTransactionAsync(userId, cardTransactionId);
            await _tagRepository.AssignToCardTransactionAsync(tagId, cardTransactionId);
        }

        public async Task UnassignFromCardTransactionAsync(int userId, int tagId, int cardTransactionId)
        {
            await GetOwnedTagAsync(userId, tagId);
            await GetOwnedCardTransactionAsync(userId, cardTransactionId);
            await _tagRepository.UnassignFromCardTransactionAsync(tagId, cardTransactionId);
        }

        public async Task<IEnumerable<TagDTO>> GetTagsForCardTransactionAsync(int userId, int cardTransactionId)
        {
            await GetOwnedCardTransactionAsync(userId, cardTransactionId);
            var tags = await _tagRepository.GetTagsForCardTransactionAsync(cardTransactionId);
            return tags.OrderBy(t => t.Name).Select(ToDto);
        }

        private async Task<Tag> GetOwnedTagAsync(int userId, int tagId)
        {
            var tag = await _tagRepository.GetByIdAsync(tagId)
                ?? throw new NotFoundException("Tag not found");
            if (tag.UserId != userId) throw new UnauthorizedDomainException();
            return tag;
        }

        private async Task<Transaction> GetOwnedTransactionAsync(int userId, int transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId)
                ?? throw new NotFoundException("Transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();
            return transaction;
        }

        private async Task<CardTransaction> GetOwnedCardTransactionAsync(int userId, int cardTransactionId)
        {
            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(cardTransactionId)
                ?? throw new NotFoundException("Card transaction not found");
            if (cardTransaction.UserId != userId) throw new UnauthorizedDomainException();
            return cardTransaction;
        }

        private static TagDTO ToDto(Tag tag) => new TagDTO
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color
        };
    }
}
