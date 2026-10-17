using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class AssistantFindingRepository : IAssistantFindingRepository
    {
        private readonly ApplicationDbContext _context;

        public AssistantFindingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AssistantFinding>> GetByUserAsync(int userId, bool onlyActive)
        {
            var query = _context.AssistantFindings
                .Where(f => f.UserId == userId && f.Status != AssistantFindingStatus.Repeated);
            if (onlyActive)
                query = query.Where(f => f.Status == AssistantFindingStatus.Active);

            // Se ordena en memoria: son pocas filas por mes y así no depende del proveedor.
            var rows = await query.ToListAsync();
            return rows.OrderByDescending(f => f.CreatedAt).ThenByDescending(f => f.Id).ToList();
        }

        public async Task<AssistantFinding?> GetByIdAsync(int userId, int id)
        {
            return await _context.AssistantFindings
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId && f.Status != AssistantFindingStatus.Repeated);
        }

        public async Task<HashSet<string>> GetFingerprintsSinceAsync(int userId, DateTime since)
        {
            var fingerprints = await _context.AssistantFindings
                .AsNoTracking()
                .Where(f => f.UserId == userId && f.CreatedAt >= since)
                .Select(f => f.Fingerprint)
                .ToListAsync();
            return fingerprints.ToHashSet();
        }

        public async Task AddRangeAsync(IEnumerable<AssistantFinding> findings)
        {
            await _context.AssistantFindings.AddRangeAsync(findings);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
