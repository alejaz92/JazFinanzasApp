using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class AssistantSchedulerRunRepository : IAssistantSchedulerRunRepository
    {
        private readonly ApplicationDbContext _context;

        public AssistantSchedulerRunRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AssistantSchedulerRun run)
        {
            await _context.AssistantSchedulerRuns.AddAsync(run);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<AssistantSchedulerRun?> GetLastAsync()
        {
            var runs = await _context.AssistantSchedulerRuns.AsNoTracking().OrderByDescending(r => r.StartedAt).Take(1).ToListAsync();
            return runs.FirstOrDefault();
        }

        public async Task<bool> AnyRunningSinceAsync(DateTime from)
        {
            return await _context.AssistantSchedulerRuns.AnyAsync(r => r.Status == AssistantSchedulerRunStatus.Running && r.StartedAt >= from);
        }
    }
}
