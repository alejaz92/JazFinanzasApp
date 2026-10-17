using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class AssistantUsageRepository : IAssistantUsageRepository
    {
        private readonly ApplicationDbContext _context;

        public AssistantUsageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssistantUserSettings?> GetSettingsAsync(int userId)
        {
            return await _context.AssistantUserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<decimal> SumCostExcludingEvalAsync(int userId, DateTime from, DateTime to)
        {
            // Se trae la lista de costos y se suma en memoria: SQL Server puede sumar decimales en una
            // sola consulta, pero EF InMemory y SQLite no, y son pocas filas por mes.
            var costs = await _context.AssistantApiCalls
                .Where(c => c.UserId == userId
                            && c.Purpose != AssistantApiCallPurpose.Eval
                            && c.CreatedAt >= from && c.CreatedAt < to)
                .Select(c => c.CostUsd)
                .ToListAsync();
            return costs.Sum();
        }

        public async Task AddApiCallAsync(AssistantApiCall call)
        {
            await _context.AssistantApiCalls.AddAsync(call);
            await _context.SaveChangesAsync();
        }
    }
}
