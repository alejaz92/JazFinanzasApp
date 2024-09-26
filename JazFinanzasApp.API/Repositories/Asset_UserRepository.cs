using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Repositories
{
    public class Asset_UserRepository : GenericRepository<Asset_User>, IAsset_UserRepository
    {
        private readonly ApplicationDbContext _context;

        public Asset_UserRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }
    }
}
