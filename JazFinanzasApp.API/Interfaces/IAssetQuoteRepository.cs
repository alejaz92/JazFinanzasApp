using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Report;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAssetQuoteRepository : IGenericRepository<AssetQuote>
    {
        Task<IEnumerable<CryptoStatsByDateDTO>> GetAssetEvolutionStats(int CryptoId, int monthsQuantity, int referenceAssetId);
        Task<AssetQuote> GetLastQuoteByAsset(int assetId, string? type);
        Task<decimal> GetQuotePrice(int assetId, DateTime date, string type);
    }
}
