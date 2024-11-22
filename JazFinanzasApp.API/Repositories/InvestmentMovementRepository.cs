using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class InvestmentMovementRepository : GenericRepository<InvestmentMovement>, IInvestmentMovementRepository
    {
        private readonly ApplicationDbContext _context;

        public InvestmentMovementRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }


        public async Task<(IEnumerable<InvestmentMovement> Movements, int TotalCount)> GetPaginatedInvestmentMovements(int userId, int page, int pageSize, string environment)
        {
            var totalCount = await _context.InvestmentMovements
                .Where(m => m.UserId == userId)
                .CountAsync();

            var movements = await _context.InvestmentMovements
                .Where(m => m.UserId == userId)
                .Where(m => m.Environment == environment)
                .Include(m => m.IncomeMovement)
                .Include(m => m.IncomeMovement.Asset)
                .Include(m => m.IncomeMovement.Asset.AssetType)
                .Include(m => m.IncomeMovement.Account)
                .Include(m => m.ExpenseMovement)
                .Include(m => m.ExpenseMovement.Asset)
                .Include(m => m.ExpenseMovement.Asset.AssetType)
                .Include(m => m.ExpenseMovement.Account)
                .OrderByDescending(m => m.Date)
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (movements, totalCount);
        }

        public async Task<InvestmentMovement> GetInvestmentMovementById(int id)
        {
            return await _context.InvestmentMovements
                .Include(m => m.IncomeMovement)
                .Include(m => m.IncomeMovement.Asset)
                .Include(m => m.IncomeMovement.Account)
                .Include(m => m.ExpenseMovement)
                .Include(m => m.ExpenseMovement.Asset)
                .Include(m => m.ExpenseMovement.Account)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

       
    }

}
