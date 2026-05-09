using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAssetQuoteRepository : IGenericRepository<AssetQuote>
    {
        Task<IEnumerable<CryptoStatsByDateResult>> GetAssetEvolutionStats(int CryptoId, int monthsQuantity, int referenceAssetId);
        Task<AssetQuote> GetLastQuoteByAsset(int assetId, string? type);
        Task<decimal> GetQuotePrice(int assetId, DateTime date, string type);
    }
}
