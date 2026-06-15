using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class PersonRepository : GenericRepository<Person>, IPersonRepository
    {
        private readonly ApplicationDbContext _context;

        public PersonRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Person>> GetByUserIdAsync(int userId)
        {
            return await _context.People
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<bool> HasActiveSplitsAsync(int personId)
        {
            return await _context.SharedExpenseSplits
                .AnyAsync(s => s.PersonId == personId && s.Status != SharedExpenseSplitStatus.Paid);
        }
    }
}
