using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class ReportFavoriteRepository : GenericRepository<ReportFavorite>, IReportFavoriteRepository
    {
        private readonly ApplicationDbContext _context;

        public ReportFavoriteRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReportFavorite>> GetByUserIdAsync(int userId)
        {
            return await _context.ReportFavorites
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }

        public async Task<ReportFavorite> GetByReportKeyAsync(string reportKey, int userId)
        {
            return await _context.ReportFavorites
                .FirstOrDefaultAsync(f => f.ReportKey == reportKey && f.UserId == userId);
        }
    }
}
