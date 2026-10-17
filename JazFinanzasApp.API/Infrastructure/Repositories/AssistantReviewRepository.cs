using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class AssistantReviewRepository : IAssistantReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public AssistantReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssistantReview?> GetByIdAsync(int userId, int id)
        {
            return await _context.AssistantReviews.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        }

        public async Task<List<AssistantReview>> GetForPeriodAsync(int userId, string kind, DateTime periodStart, string purpose)
        {
            return await _context.AssistantReviews
                .Where(r => r.UserId == userId && r.Kind == kind && r.PeriodStart == periodStart && r.Purpose == purpose)
                .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task<List<AssistantReview>> GetCurrentUserReviewsAsync(int userId, string? kind)
        {
            var query = _context.AssistantReviews
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Purpose == AssistantReviewPurpose.User && r.IsCurrent
                            && r.Kind != AssistantReviewKind.WeeklyScan
                            && r.Status != AssistantReviewStatus.NoActivity);
            if (!string.IsNullOrEmpty(kind))
                query = query.Where(r => r.Kind == kind);

            // Se ordena en memoria: son pocas filas por año y así no depende del proveedor.
            var rows = await query.ToListAsync();
            return rows.OrderByDescending(r => r.PeriodStart).ThenByDescending(r => r.Kind).ThenByDescending(r => r.Id).ToList();
        }

        public async Task<AssistantReview?> GetLatestReadyAsync(int userId)
        {
            var rows = await _context.AssistantReviews
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Purpose == AssistantReviewPurpose.User && r.IsCurrent
                            && r.Status == AssistantReviewStatus.Ready && r.Kind != AssistantReviewKind.WeeklyScan)
                .ToListAsync();
            return rows.OrderByDescending(r => r.PeriodStart).ThenByDescending(r => r.Kind).ThenByDescending(r => r.Id).FirstOrDefault();
        }

        public async Task<bool> HasActivityAsync(int userId, DateTime from, DateTime to)
        {
            var end = to.Date.AddDays(1);
            var hasTransactions = await _context.Transactions
                .AnyAsync(t => t.Account.UserId == userId && t.Date >= from.Date && t.Date < end);
            if (hasTransactions) return true;
            return await _context.CardTransactions
                .AnyAsync(c => c.UserId == userId && c.Date >= from.Date && c.Date < end);
        }

        public async Task<List<AssistantReview>> GetUserPassesSinceAsync(int userId, DateTime since)
        {
            return await _context.AssistantReviews
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Purpose == AssistantReviewPurpose.User && r.CreatedAt >= since)
                .ToListAsync();
        }

        public async Task<DateTime?> GetLastReadyPassTimeAsync(int userId)
        {
            var times = await _context.AssistantReviews
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Purpose == AssistantReviewPurpose.User && r.Status == AssistantReviewStatus.Ready)
                .Select(r => r.CreatedAt)
                .ToListAsync();
            return times.Count == 0 ? null : times.Max();
        }

        public async Task<bool> HasMovementsLoadedSinceAsync(int userId, DateTime since)
        {
            if (await _context.Transactions.AnyAsync(t => t.Account.UserId == userId && t.CreatedAt > since))
                return true;
            return await _context.CardTransactions.AnyAsync(c => c.UserId == userId && c.CreatedAt > since);
        }

        public async Task<List<AssistantQualityReview>> GetLastQualityReviewsAsync(int userId, int take)
        {
            var rows = await _context.AssistantQualityReviews.AsNoTracking().Where(q => q.UserId == userId).ToListAsync();
            return rows.OrderByDescending(q => q.ReviewedAt).ThenByDescending(q => q.Id).Take(take).ToList();
        }

        public async Task<DateTime?> GetFirstReadyReviewTimeAsync(int userId)
        {
            var times = await ReadyReviewTimesAsync(userId);
            return times.Count == 0 ? null : times.Min();
        }

        public async Task<int> CountReadyReviewsSinceAsync(int userId, DateTime since)
        {
            var times = await ReadyReviewTimesAsync(userId);
            return times.Count(t => t > since);
        }

        private async Task<List<DateTime>> ReadyReviewTimesAsync(int userId)
        {
            return await _context.AssistantReviews
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Purpose == AssistantReviewPurpose.User && r.Status == AssistantReviewStatus.Ready
                            && r.Kind != AssistantReviewKind.WeeklyScan)
                .Select(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<int, AssistantFeedback>> GetFeedbackByReviewIdsAsync(IEnumerable<int> reviewIds)
        {
            var ids = reviewIds.ToList();
            return await _context.AssistantFeedbacks
                .Where(f => ids.Contains(f.ReviewId))
                .ToDictionaryAsync(f => f.ReviewId);
        }

        public async Task AddAsync(AssistantReview review)
        {
            await _context.AssistantReviews.AddAsync(review);
            await _context.SaveChangesAsync();
        }

        public async Task AddFeedbackAsync(AssistantFeedback feedback)
        {
            await _context.AssistantFeedbacks.AddAsync(feedback);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
