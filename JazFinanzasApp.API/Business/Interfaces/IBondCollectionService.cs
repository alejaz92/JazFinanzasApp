using JazFinanzasApp.API.Business.DTO.BondCollection;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IBondCollectionService
    {
        Task<IEnumerable<BondCollectionPendingDTO>> GetPendingAsync(int userId);
        Task<IEnumerable<BondCollectionListDTO>> GetRegisteredAsync(int userId);
        Task<int> RegisterAsync(int userId, BondCollectionRegisterDTO dto);
        Task DismissAsync(int userId, BondCollectionDismissDTO dto);
        Task DeleteAsync(int userId, int id);
        Task<int?> GetLastInterestClassIdAsync(int userId);
    }
}
