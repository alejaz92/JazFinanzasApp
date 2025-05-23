using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class PortfolioRepository : GenericRepository<Portfolio>, IPortfolioRepository
    {
        private readonly ApplicationDbContext _context;
        public PortfolioRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }


        //check if portfolio used in transactions
        public async Task<bool> IsPortfolioUsedInTransactions(int portfolioId) {
            return await _context.Transactions.AnyAsync(t => t.PortfolioId == portfolioId);
        }

        // get users default portfolio
        public async Task<Portfolio> GetDefaultPortfolio(int userId)
        {
            return await _context.Portfolios
                .Where(p => p.UserId == userId)
                .Where(p => p.IsDefault)
                .FirstOrDefaultAsync();
        }
    }
}
