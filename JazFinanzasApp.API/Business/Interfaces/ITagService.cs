using JazFinanzasApp.API.Business.DTO.Tag;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ITagService
    {
        Task<IEnumerable<TagDTO>> GetAllForUserAsync(int userId);
        Task<TagDTO> GetByIdAsync(int userId, int id);
        Task CreateTagAsync(int userId, TagDTO dto);
        Task UpdateTagAsync(int userId, int id, TagDTO dto);
        Task DeleteTagAsync(int userId, int id);

        Task AssignToTransactionAsync(int userId, int tagId, int transactionId);
        Task UnassignFromTransactionAsync(int userId, int tagId, int transactionId);
        Task<IEnumerable<TagDTO>> GetTagsForTransactionAsync(int userId, int transactionId);

        Task AssignToCardTransactionAsync(int userId, int tagId, int cardTransactionId);
        Task UnassignFromCardTransactionAsync(int userId, int tagId, int cardTransactionId);
        Task<IEnumerable<TagDTO>> GetTagsForCardTransactionAsync(int userId, int cardTransactionId);
    }
}
