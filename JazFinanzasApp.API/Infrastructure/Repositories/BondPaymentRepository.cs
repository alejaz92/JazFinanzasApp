using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class BondPaymentRepository : IBondPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public BondPaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BondPayment>> GetByAssetIdAsync(int assetId)
        {
            return await _context.BondPayments
                .Where(bp => bp.AssetId == assetId)
                .Include(bp => bp.Asset)
                .Include(bp => bp.CurrencyAsset)
                .OrderBy(bp => bp.PaymentDate)
                .ToListAsync();
        }

        public async Task<BondPayment> GetByIdAsync(int id)
        {
            return await _context.BondPayments
                .Include(bp => bp.Asset)
                .Include(bp => bp.CurrencyAsset)
                .FirstOrDefaultAsync(bp => bp.Id == id);
        }

        public async Task<IEnumerable<BondPayment>> GetPastAsync(DateTime asOf)
        {
            return await _context.BondPayments
                .Where(bp => bp.PaymentDate <= asOf)
                .Include(bp => bp.Asset)
                .Include(bp => bp.CurrencyAsset)
                .OrderBy(bp => bp.PaymentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BondPayment>> GetFutureAsync(DateTime asOf)
        {
            return await _context.BondPayments
                .Where(bp => bp.PaymentDate > asOf)
                .Include(bp => bp.Asset)
                .Include(bp => bp.CurrencyAsset)
                .OrderBy(bp => bp.PaymentDate)
                .ToListAsync();
        }
    }
}
