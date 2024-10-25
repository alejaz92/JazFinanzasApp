using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Asset;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace JazFinanzasApp.API.Repositories
{
    public class AssetRepository : GenericRepository<Asset>, IAssetRepository
    {
        private readonly ApplicationDbContext _context;

        public AssetRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Asset>> GetAssetsAsync()
        {
            return await _context.Assets.Include(a => a.AssetType).ToListAsync();
        }
        
        public async Task<IEnumerable<Asset>> GetAssetsByTypeAsync(int assetTypeId)
        {
            return await _context.Assets
                .Where(a => a.AssetTypeId == assetTypeId)
                .Include(a => a.AssetType)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<Asset> GetAssetByIdAsync(int id)
        {
            return await _context.Assets
                .Include(a => a.AssetType)
                .OrderBy(a => a.Name)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        // get asset by name
        public async Task<Asset> GetAssetByNameAsync(string name)
        {
            return await _context.Assets
                .Include(a => a.AssetType)
                .FirstOrDefaultAsync(a => a.Name == name);
        }




        

    }
}
