using JazFinanzasApp.API.Business.DTO.Tag;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<IEnumerable<TagDTO>> GetAllForUserAsync(int userId)
        {
            var tags = await _tagRepository.GetByUserIdAsync(userId);
            return tags.Select(ToDTO);
        }

        public async Task<TagDTO> CreateTagAsync(int userId, TagAddDTO dto)
        {
            var existing = await _tagRepository.FindAsync(t => t.Name == dto.Name && t.UserId == userId);
            if (existing.Any()) throw new BusinessRuleException("Ya existe una etiqueta con ese nombre");

            var tag = new Tag { Name = dto.Name, Color = dto.Color, UserId = userId };
            var created = await _tagRepository.AddAsyncReturnObject(tag);
            return ToDTO(created);
        }

        public async Task UpdateTagAsync(int userId, int id, TagEditDTO dto)
        {
            var tag = await _tagRepository.GetByIdAsync(id) ?? throw new NotFoundException("Tag not found");
            if (tag.UserId != userId) throw new UnauthorizedDomainException();

            var duplicate = await _tagRepository.FindAsync(t => t.Name == dto.Name && t.UserId == userId && t.Id != id);
            if (duplicate.Any()) throw new BusinessRuleException("Ya existe una etiqueta con ese nombre");

            tag.Name = dto.Name;
            tag.Color = dto.Color;
            tag.UpdatedAt = DateTime.UtcNow;
            await _tagRepository.UpdateAsync(tag);
        }

        // Borra las asignaciones junto con el tag en vez de bloquear el borrado — a diferencia
        // de una categoría, una etiqueta es decorativa (sección 7 del plan), así que exigir
        // desasignarla movimiento por movimiento antes de poder borrarla sería fricción sin
        // ningún beneficio real.
        public async Task DeleteTagAsync(int userId, int id)
        {
            var tag = await _tagRepository.GetByIdAsync(id) ?? throw new NotFoundException("Tag not found");
            if (tag.UserId != userId) throw new UnauthorizedDomainException();
            await _tagRepository.DeleteTagWithAssignmentsAsync(id);
        }

        public async Task AssignToTransactionAsync(int userId, int transactionId, int tagId)
        {
            await ValidateTagOwnershipAsync(userId, tagId);
            await ValidateTransactionOwnershipAsync(userId, transactionId);

            if (await _tagRepository.IsAssignedToTransactionAsync(transactionId, tagId))
                throw new BusinessRuleException("La etiqueta ya está asignada a este movimiento");

            await _tagRepository.AssignToTransactionAsync(transactionId, tagId);
        }

        public async Task UnassignFromTransactionAsync(int userId, int transactionId, int tagId)
        {
            await ValidateTagOwnershipAsync(userId, tagId);
            await ValidateTransactionOwnershipAsync(userId, transactionId);
            await _tagRepository.UnassignFromTransactionAsync(transactionId, tagId);
        }

        public async Task AssignToCardTransactionAsync(int userId, int cardTransactionId, int tagId)
        {
            await ValidateTagOwnershipAsync(userId, tagId);
            await ValidateCardTransactionOwnershipAsync(userId, cardTransactionId);

            if (await _tagRepository.IsAssignedToCardTransactionAsync(cardTransactionId, tagId))
                throw new BusinessRuleException("La etiqueta ya está asignada a este consumo");

            await _tagRepository.AssignToCardTransactionAsync(cardTransactionId, tagId);
        }

        public async Task UnassignFromCardTransactionAsync(int userId, int cardTransactionId, int tagId)
        {
            await ValidateTagOwnershipAsync(userId, tagId);
            await ValidateCardTransactionOwnershipAsync(userId, cardTransactionId);
            await _tagRepository.UnassignFromCardTransactionAsync(cardTransactionId, tagId);
        }

        public async Task<IEnumerable<TagDTO>> GetTagsForTransactionAsync(int userId, int transactionId)
        {
            await ValidateTransactionOwnershipAsync(userId, transactionId);
            var tags = await _tagRepository.GetTagsForTransactionAsync(transactionId);
            return tags.Select(ToDTO);
        }

        public async Task<IEnumerable<TagDTO>> GetTagsForCardTransactionAsync(int userId, int cardTransactionId)
        {
            await ValidateCardTransactionOwnershipAsync(userId, cardTransactionId);
            var tags = await _tagRepository.GetTagsForCardTransactionAsync(cardTransactionId);
            return tags.Select(ToDTO);
        }

        private async Task ValidateTagOwnershipAsync(int userId, int tagId)
        {
            var tag = await _tagRepository.GetByIdAsync(tagId) ?? throw new NotFoundException("Tag not found");
            if (tag.UserId != userId) throw new UnauthorizedDomainException();
        }

        private async Task ValidateTransactionOwnershipAsync(int userId, int transactionId)
        {
            var ownerId = await _tagRepository.GetTransactionOwnerIdAsync(transactionId)
                ?? throw new NotFoundException("Transaction not found");
            if (ownerId != userId) throw new UnauthorizedDomainException();
        }

        private async Task ValidateCardTransactionOwnershipAsync(int userId, int cardTransactionId)
        {
            var ownerId = await _tagRepository.GetCardTransactionOwnerIdAsync(cardTransactionId)
                ?? throw new NotFoundException("Card transaction not found");
            if (ownerId != userId) throw new UnauthorizedDomainException();
        }

        private static TagDTO ToDTO(Tag t) => new TagDTO { Id = t.Id, Name = t.Name, Color = t.Color };
    }
}
