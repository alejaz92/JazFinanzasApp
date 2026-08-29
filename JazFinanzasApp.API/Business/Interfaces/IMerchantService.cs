using JazFinanzasApp.API.Business.DTO.Merchant;
using JazFinanzasApp.API.Business.Services;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IMerchantService
    {
        Task<IEnumerable<MerchantListItemDTO>> GetAllForUserAsync(int userId);
        Task RenameMerchantAsync(int userId, int id, MerchantRenameDTO dto);
        Task MergeMerchantsAsync(int userId, int sourceMerchantId, int targetMerchantId);
        Task ReassignTransactionAsync(int userId, int transactionId, int merchantId);
        Task ReassignCardTransactionAsync(int userId, int cardTransactionId, int merchantId);
        Task<MerchantResolveBulkResultDTO> ResolveAllAsync(int userId, int minOccurrences = MerchantService.DefaultMinOccurrences);
        Task<IEnumerable<MerchantMovementDTO>> GetMovementsAsync(int userId, int merchantId);
    }
}
