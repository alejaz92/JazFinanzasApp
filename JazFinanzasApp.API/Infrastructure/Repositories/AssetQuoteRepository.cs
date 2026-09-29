using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class AssetQuoteRepository: GenericRepository<AssetQuote>, IAssetQuoteRepository
    {
        private readonly ApplicationDbContext _context;
        public AssetQuoteRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<decimal> GetQuotePrice(int assetId, DateTime date, string type)
        {
            var quote = await _context.AssetQuotes
                .Where(aq => aq.Asset.Id == assetId)
                .Where(aq => aq.Date <= date)
                .Where(aq => aq.Type == type)
                .OrderByDescending(aq => aq.Date)
                .FirstOrDefaultAsync();

            return quote.Value;
        }

        public async Task<AssetQuote> GetLastQuoteByAsset(int assetId, string? type)
        {

            if (type != null)
            {
                return await _context.AssetQuotes
                    .Where(aq => aq.Asset.Id == assetId)
                    .Where(aq => aq.Type == type)
                    .OrderByDescending(aq => aq.Date)
                    .FirstOrDefaultAsync();
            } else
            {
                return await _context.AssetQuotes
                .Where(aq => aq.Asset.Id == assetId)
                .OrderByDescending(aq => aq.Date)
                .FirstOrDefaultAsync();
            }
            
        }

        // plan-alerta-cotizaciones, T4: mismo filtro de tipo de cotización que GetStaleAssetsAsync
        // (TransactionRepository) y mismo pivote dólar excluido (Id 2, "NetWorthDollarPivotAssetId"
        // allá) — ese activo nunca tiene cotización propia, así que su familia (Moneda) no debe
        // depender de él para saber si está frenada.
        public async Task<Dictionary<string, DateTime>> GetLatestQuoteDateByAssetTypeAsync()
        {
            return await _context.AssetQuotes
                .Where(q => q.Type != "TARJETA" && q.Type != "BLUE")
                .Where(q => q.AssetId != 2)
                .GroupBy(q => q.Asset.AssetType.Name)
                .Select(g => new { AssetTypeName = g.Key, LastDate = g.Max(q => q.Date) })
                .ToDictionaryAsync(g => g.AssetTypeName, g => g.LastDate);
        }

        public async Task<IEnumerable<CryptoStatsByDateResult>> GetAssetEvolutionStats(int CryptoId, int monthsQuantity, int referenceAssetId)
        { 
            var dateThreshold = DateTime.Now.AddMonths(-monthsQuantity);

            var result = await _context.AssetQuotes
                .Where(aq => aq.Asset.Id == CryptoId)
                .Where(aq => aq.Date >= dateThreshold)
                .OrderBy(aq => aq.Date)
                .Select(aq => new CryptoStatsByDateResult
                {
                    Date = aq.Date,
                    Value = 1 / aq.Value * _context.AssetQuotes
                        .Where(aq2 => aq2.Asset.Id == referenceAssetId)
                        .Where(aq2 => aq2.Type == "NA" || aq2.Type == "BLUE")
                        .Where(aq2 => aq2.Date <= aq.Date)
                        .OrderByDescending(aq2 => aq2.Date)
                        .Select(aq2 => aq2.Value)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return result;
        }
    }
}
