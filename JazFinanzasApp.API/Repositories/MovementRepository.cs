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
            var totalCount = await _context.Movements
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.MovementClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I")
                .CountAsync();

            var movements = await _context.Movements
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.MovementClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I") 
                .Include(m => m.Account)
                .Include(m => m.Asset)
                .Include(m => m.MovementClass)
                .OrderByDescending(m => m.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (movements, totalCount);
        }

        public async Task<int> GetNextId()
        {
            var lastMovement = await _context.Movements.OrderByDescending(m => m.Id).FirstOrDefaultAsync();
            return lastMovement.Id + 1;
        }

    }
}
