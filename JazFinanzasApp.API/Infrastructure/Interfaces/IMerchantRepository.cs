using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IMerchantRepository : IGenericRepository<Merchant>
    {
        // Lo que necesita MerchantResolver (Fase 8a).
        Task<MerchantAlias?> FindAliasAsync(int userId, string normalizedDetail);
        Task<Merchant> CreateMerchantWithAliasAsync(int userId, string name, string normalizedDetail);

        // Lo que necesita MerchantService (Fase 8b).
        Task<IEnumerable<Merchant>> GetByUserIdAsync(int userId);
        Task<Dictionary<int, int>> GetVolumesByMerchantAsync(int userId);
        Task<IEnumerable<Transaction>> GetUnresolvedTransactionsAsync(int userId);
        Task<IEnumerable<CardTransaction>> GetUnresolvedCardTransactionsAsync(int userId);
        Task SetTransactionMerchantAsync(int transactionId, int? merchantId);
        Task SetCardTransactionMerchantAsync(int cardTransactionId, int? merchantId);

        // Crea o actualiza el alias de (usuario del merchant, normalizedDetail) apuntando a
        // merchantId, marcado IsManual — usado al reasignar un movimiento a mano (T7: la
        // corrección se propaga al texto normalizado, no solo a ese movimiento puntual).
        Task UpsertManualAliasAsync(int merchantId, string normalizedDetail);

        // Reasigna movimientos y alias de sourceMerchantId a targetMerchantId y borra el origen.
        // Un alias del origen cuyo texto ya existe en el destino se descarta (no se duplica) en
        // vez de reasignarse — el destino ya cubre ese texto.
        Task MergeAsync(int sourceMerchantId, int targetMerchantId);
    }
}
