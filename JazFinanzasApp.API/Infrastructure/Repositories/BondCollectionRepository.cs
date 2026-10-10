using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class BondCollectionRepository : IBondCollectionRepository
    {
        private readonly ApplicationDbContext _context;

        public BondCollectionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<BondCollection> WithIncludes() =>
            _context.BondCollections
                .Include(bc => bc.BondPayment).ThenInclude(bp => bp.Asset)
                .Include(bc => bc.BondPayment).ThenInclude(bp => bp.CurrencyAsset)
                .Include(bc => bc.Account)
                .Include(bc => bc.Portfolio);

        public async Task<BondCollection> GetByIdAsync(int id)
        {
            return await WithIncludes().FirstOrDefaultAsync(bc => bc.Id == id);
        }

        public async Task<IEnumerable<BondCollection>> GetByUserIdAsync(int userId)
        {
            return await WithIncludes()
                .Where(bc => bc.UserId == userId)
                .OrderByDescending(bc => bc.BondPayment.PaymentDate)
                .ToListAsync();
        }

        public async Task<BondCollection> AddAsyncReturnObject(BondCollection collection)
        {
            await _context.BondCollections.AddAsync(collection);
            await _context.SaveChangesAsync();
            return collection;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.BondCollections.FindAsync(id);
            if (entity != null)
            {
                _context.BondCollections.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
