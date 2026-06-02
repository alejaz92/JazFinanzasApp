using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class AssetSplitEventRepository : IAssetSplitEventRepository
    {
        private readonly ApplicationDbContext _context;

        public AssetSplitEventRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AssetSplitEvent>> GetByAssetIdAsync(int assetId)
        {
            return await _context.AssetSplitEvents
                .Where(s => s.AssetId == assetId)
                .Include(s => s.Asset)
                .OrderByDescending(s => s.Date)
                .ToListAsync();
        }

        public async Task<AssetSplitEvent> GetByIdAsync(int id)
        {
            return await _context.AssetSplitEvents
                .Include(s => s.Asset)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(AssetSplitEvent splitEvent)
        {
            await _context.AssetSplitEvents.AddAsync(splitEvent);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.AssetSplitEvents.FindAsync(id);
            if (entity != null)
            {
                _context.AssetSplitEvents.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
