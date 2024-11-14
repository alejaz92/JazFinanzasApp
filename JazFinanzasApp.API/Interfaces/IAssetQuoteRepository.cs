using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IAssetQuoteRepository : IGenericRepository<AssetQuote>
    {
        Task<AssetQuote> GetLastQuoteByAsset(int assetId, string? type);
        Task<decimal> GetQuotePrice(int assetId, DateTime date, string type);
    }
}
