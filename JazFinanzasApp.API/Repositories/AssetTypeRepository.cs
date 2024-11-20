using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class AssetTypeRepository : GenericRepository<AssetType>, IAssetTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public AssetTypeRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }

        // get id by name   
        public async Task<int> GetIdByName(string name) {
            var assetType = await _context.AssetTypes.FirstOrDefaultAsync(a => a.Name == name);
            return assetType.Id;
        }

        // get asset type by name
        public async Task<AssetType> GetByName(string name) {
            return await _context.AssetTypes.FirstOrDefaultAsync(a => a.Name == name);
        }

        // get asset types by environment
        public async Task<IEnumerable<AssetType>> GetAssetTypes(string environment) {
            return await _context.AssetTypes.Where(a => a.Environment == environment).ToListAsync();
        }
    }
}
