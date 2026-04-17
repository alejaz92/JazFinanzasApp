using JazFinanzasApp.API.Business.DTO.Report;
using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IAssetQuoteRepository : IGenericRepository<AssetQuote>
    {
        Task<IEnumerable<CryptoStatsByDateDTO>> GetAssetEvolutionStats(int CryptoId, int monthsQuantity, int referenceAssetId);
        Task<AssetQuote> GetLastQuoteByAsset(int assetId, string? type);
        Task<decimal> GetQuotePrice(int assetId, DateTime date, string type);
    }
}
