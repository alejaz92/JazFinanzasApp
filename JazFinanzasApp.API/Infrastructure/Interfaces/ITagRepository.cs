using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<IEnumerable<Tag>> GetByUserIdAsync(int userId);

        Task<int?> GetTransactionOwnerIdAsync(int transactionId);
        Task<int?> GetCardTransactionOwnerIdAsync(int cardTransactionId);

        Task<bool> IsAssignedToTransactionAsync(int transactionId, int tagId);
        Task<bool> IsAssignedToCardTransactionAsync(int cardTransactionId, int tagId);

        Task AssignToTransactionAsync(int transactionId, int tagId);
        Task UnassignFromTransactionAsync(int transactionId, int tagId);
        Task AssignToCardTransactionAsync(int cardTransactionId, int tagId);
        Task UnassignFromCardTransactionAsync(int cardTransactionId, int tagId);

        Task<IEnumerable<Tag>> GetTagsForTransactionAsync(int transactionId);
        Task<IEnumerable<Tag>> GetTagsForCardTransactionAsync(int cardTransactionId);

        // Borra el tag junto con todas sus asignaciones (Transaction/CardTransaction) — el FK
        // hacia Tag es NoAction (ver ApplicationDbContext), así que la limpieza es manual acá,
        // mismo criterio que TripSuggestionDismissal con su Trip.
        Task DeleteTagWithAssignmentsAsync(int tagId);
    }
}
