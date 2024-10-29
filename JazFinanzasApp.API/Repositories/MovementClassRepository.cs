using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class MovementClassRepository : GenericRepository<MovementClass>, IMovementClassRepository
    {
        private readonly ApplicationDbContext _context;

        public MovementClassRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }



        public async Task<MovementClass> GetMovementClassByDescriptionAsync(string Description)
        {
            return await _context.MovementClasses
                .FirstOrDefaultAsync(m => m.Description == Description);
        }
    }
}
