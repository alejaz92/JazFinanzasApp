using JazFinanzasApp.API.Business.DTO.Report;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Domain;
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

        public async Task<IEnumerable<CryptoStatsByDateDTO>> GetAssetEvolutionStats(int CryptoId, int monthsQuantity, int referenceAssetId)
        { 
            var dateThreshold = DateTime.Now.AddMonths(-monthsQuantity);

            var result = await _context.AssetQuotes
                .Where(aq => aq.Asset.Id == CryptoId)
                .Where(aq => aq.Date >= dateThreshold)
                .OrderBy(aq => aq.Date)
                .Select(aq => new CryptoStatsByDateDTO
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
