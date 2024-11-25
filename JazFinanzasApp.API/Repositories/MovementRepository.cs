using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class MovementRepository : GenericRepository<Movement>, IMovementRepository
    {
        private readonly ApplicationDbContext _context;

        public MovementRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Movement> Movements, int TotalCount)> GetPaginatedMovements(int userId, int page, int pageSize)
        {
            //var excludedIds = await _context.InvestmentMovements
            //    .SelectMany(im => new[] { im.IncomeMovementId, im.ExpenseMovementId })
            //    .Where(id => id.HasValue)
            //    .Select(id => id.Value)
            //    .ToListAsync();


            var totalCount = await _context.Movements
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.TransactionClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I")
                .Where(m => !_context.InvestmentMovements.Any(im => im.IncomeMovementId == m.Id || im.ExpenseMovementId == m.Id))
                .CountAsync();

            var movements = await _context.Movements
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.TransactionClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I")
                .Where(m => !_context.InvestmentMovements.Any(im => im.IncomeMovementId == m.Id || im.ExpenseMovementId == m.Id))
                .Include(m => m.Account)
                .Include(m => m.Asset)
                .Include(m => m.TransactionClass)
                .OrderByDescending(m => m.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (movements, totalCount);
        }


        // get by id including related tables
        public async Task<Movement> GetMovementByIdAsync(int id)
        {
            return await _context.Movements
                .Include(m => m.Account)
                .Include(m => m.Asset)
                .Include(m => m.TransactionClass)
                .FirstOrDefaultAsync(m => m.Id == id);
        }
        

    }
}
