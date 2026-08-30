using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<Tag> GetByNameAsync(string name, int userId);
        Task<IEnumerable<Tag>> GetByUserIdAsync(int userId);

        Task<bool> IsAssignedToTransactionAsync(int tagId, int transactionId);
        Task AssignToTransactionAsync(int tagId, int transactionId);
        Task UnassignFromTransactionAsync(int tagId, int transactionId);
        Task<IEnumerable<Tag>> GetTagsForTransactionAsync(int transactionId);

        Task<bool> IsAssignedToCardTransactionAsync(int tagId, int cardTransactionId);
        Task AssignToCardTransactionAsync(int tagId, int cardTransactionId);
        Task UnassignFromCardTransactionAsync(int tagId, int cardTransactionId);
        Task<IEnumerable<Tag>> GetTagsForCardTransactionAsync(int cardTransactionId);
    }
}
