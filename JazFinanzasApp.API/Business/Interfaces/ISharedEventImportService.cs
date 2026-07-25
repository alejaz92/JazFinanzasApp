using JazFinanzasApp.API.Business.DTO.SharedEvent.Import;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface ISharedEventImportService
    {
        Task<SharedEventImportParseResultDTO> ParseAsync(int userId, SharedEventImportParseDTO dto);
        Task<SharedEventImportConfirmResultDTO> ConfirmAsync(int userId, int sharedEventId, SharedEventImportConfirmDTO dto);
    }
}
