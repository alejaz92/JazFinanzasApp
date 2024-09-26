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

        public async Task<IEnumerable<AssetDTO>> GetAllAssetsAsync()
        {
            return await _context.Assets
                .Include(a => a.AssetType)
                .Select(a => new AssetDTO
                {
                    Name = a.Name,
                    Symbol = a.Symbol,
                    AssetTypeName = a.AssetType.Name
                })
                .ToListAsync();
                
        }
    }
}
