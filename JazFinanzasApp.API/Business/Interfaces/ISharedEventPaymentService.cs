using JazFinanzasApp.API.Business.DTO.SharedEvent;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ISharedEventPaymentService
    {
        Task<SharedEventPaymentPreviewDTO> PreviewPaymentAsync(int userId, int sharedEventId, SharedEventPaymentAddDTO dto);
        Task<SharedEventPaymentDTO> CreatePaymentAsync(int userId, int sharedEventId, SharedEventPaymentAddDTO dto);
        Task DeletePaymentAsync(int userId, int sharedEventId, int paymentId);
    }
}
