using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class MovementClassRepository : IMovementClassRepository
    {
        private readonly ApplicationDbContext _context;

        public MovementClassRepository(ApplicationDbContext context)
        {
            _context = context;
        }

    }
}
