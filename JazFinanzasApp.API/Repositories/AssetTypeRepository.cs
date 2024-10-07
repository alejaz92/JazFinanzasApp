using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Repositories
{
    public class AssetTypeRepository : GenericRepository<AssetType>, IAssetTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public AssetTypeRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }
    }
}
