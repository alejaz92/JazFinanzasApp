using JazFinanzasApp.API.Business.DTO.TransactionClass;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ITransactionClassService
    {
        Task<IEnumerable<TransactionClassDTO>> GetAllForUserAsync(int userId);
        Task<TransactionClassDTO> GetByIdAsync(int userId, int id);
        Task CreateTransactionClassAsync(int userId, TransactionClassDTO dto);
        Task UpdateTransactionClassAsync(int userId, int id, TransactionClassDTO dto);
        Task DeleteTransactionClassAsync(int userId, int id);
    }
}
