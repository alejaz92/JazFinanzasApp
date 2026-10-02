using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IBondCollectionRepository
    {
        // Con BondPayment.Asset, Account y Portfolio cargados — lo que necesita el mapeo a DTO.
        Task<BondCollection> GetByIdAsync(int id);
        Task<IEnumerable<BondCollection>> GetByUserIdAsync(int userId);
        Task<BondCollection> AddAsyncReturnObject(BondCollection collection);
        Task DeleteAsync(int id);
    }
}
