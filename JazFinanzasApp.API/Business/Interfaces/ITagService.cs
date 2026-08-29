using JazFinanzasApp.API.Business.DTO.Tag;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ITagService
    {
        Task<IEnumerable<TagDTO>> GetAllForUserAsync(int userId);
        Task<TagDTO> CreateTagAsync(int userId, TagAddDTO dto);
        Task UpdateTagAsync(int userId, int id, TagEditDTO dto);
        Task DeleteTagAsync(int userId, int id);

        Task AssignToTransactionAsync(int userId, int transactionId, int tagId);
        Task UnassignFromTransactionAsync(int userId, int transactionId, int tagId);
        Task AssignToCardTransactionAsync(int userId, int cardTransactionId, int tagId);
        Task UnassignFromCardTransactionAsync(int userId, int cardTransactionId, int tagId);

        Task<IEnumerable<TagDTO>> GetTagsForTransactionAsync(int userId, int transactionId);
        Task<IEnumerable<TagDTO>> GetTagsForCardTransactionAsync(int userId, int cardTransactionId);
    }
}
