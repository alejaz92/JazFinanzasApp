using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
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
    }
}
