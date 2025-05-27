using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class Asset_UserRepository : GenericRepository<Asset_User>, IAsset_UserRepository
    {
        private readonly ApplicationDbContext _context;

        public Asset_UserRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }

        public async Task<IEnumerable<Asset_User>> GetUserAssetsAsync(int userId, int assetTypeId)
        {
            return await _context.Assets_Users
                .Include(au => au.Asset)
                .Include(au => au.Asset.AssetType)
                .Where(au => au.UserId == userId)   
                .Where(au => au.Asset.AssetType.Id == assetTypeId)
                .ToListAsync();
        }

        public async Task AssignAssetToUserAsync(int userId, int assetId)
        {
            var assetUser = new Asset_User
            {
                UserId = userId,
                AssetId = assetId
            };

            await _context.Assets_Users.AddAsync(assetUser);
            await _context.SaveChangesAsync();
        }

        public async Task UnassignAssetToUserAsync(int userId, int assetId)
        {
            var assetUser = await _context.Assets_Users
                .FirstOrDefaultAsync(au => au.UserId == userId && au.AssetId == assetId);

            if (assetUser != null)
            {
                _context.Assets_Users.Remove(assetUser);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<Asset_User> GetUserAssetAsync(int userId, int assetId)
        {
            return await _context.Assets_Users
                .FirstOrDefaultAsync(au => au.UserId == userId && au.AssetId == assetId);
        }

        public async Task<IEnumerable<Asset_User>> GetReferenceAssetsAsync(int userId)
        {
            return await _context.Assets_Users
                .Include(au => au.Asset)
                .Include(au => au.Asset.AssetType)
                .Where(au => au.UserId == userId)
                .Where(au => au.isReference)
                .ToListAsync();            
        }

        public async Task<bool> IsAssetUserInUseAsync(int userId, int assetId)
        {
            return await _context.Transactions
                .AnyAsync(t => t.UserId == userId && t.AssetId == assetId) ||
                await _context.CardTransactions
                .AnyAsync(ct => ct.UserId == userId && ct.AssetId == assetId);
        }

        public async Task<Asset_User> GetMainReferenceAssetAsync(int userId)
        {
            return await _context.Assets_Users
                .Include(au => au.Asset)
                .Include(au => au.Asset.AssetType)
                .FirstOrDefaultAsync(au => au.UserId == userId && au.isMainReference);
        }


    }
}
