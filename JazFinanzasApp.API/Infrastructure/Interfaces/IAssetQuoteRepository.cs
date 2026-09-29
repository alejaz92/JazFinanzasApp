using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAssetQuoteRepository : IGenericRepository<AssetQuote>
    {
        Task<IEnumerable<CryptoStatsByDateResult>> GetAssetEvolutionStats(int CryptoId, int monthsQuantity, int referenceAssetId);
        Task<AssetQuote> GetLastQuoteByAsset(int assetId, string? type);
        Task<decimal> GetQuotePrice(int assetId, DateTime date, string type);

        // plan-alerta-cotizaciones, T4: última fecha de cotización de TODO el catálogo por tipo de
        // activo (no solo lo que un usuario tiene) — para distinguir "se cayó la fuente entera de
        // bonos" de "a un activo suelto le falta un día", sin importar quién lo tenga en cartera.
        Task<Dictionary<string, DateTime>> GetLatestQuoteDateByAssetTypeAsync();
    }
}
