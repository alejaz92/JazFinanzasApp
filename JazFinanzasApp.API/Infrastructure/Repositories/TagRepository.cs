using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        private readonly ApplicationDbContext _context;

        public TagRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Tag> GetByNameAsync(string name, int userId)
        {
            return await _context.Tags
                .FirstOrDefaultAsync(t => t.Name == name && t.UserId == userId);
        }

        public async Task<bool> IsAssignedToTransactionAsync(int tagId, int transactionId)
        {
            return await _context.TransactionTags
                .AnyAsync(tt => tt.TagId == tagId && tt.TransactionId == transactionId);
        }

        public async Task AssignToTransactionAsync(int tagId, int transactionId)
        {
            if (await IsAssignedToTransactionAsync(tagId, transactionId)) return;
            await _context.TransactionTags.AddAsync(new TransactionTag { TagId = tagId, TransactionId = transactionId });
            await _context.SaveChangesAsync();
        }

        public async Task UnassignFromTransactionAsync(int tagId, int transactionId)
        {
            var link = await _context.TransactionTags
                .FirstOrDefaultAsync(tt => tt.TagId == tagId && tt.TransactionId == transactionId);
            if (link == null) return;
            _context.TransactionTags.Remove(link);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Tag>> GetTagsForTransactionAsync(int transactionId)
        {
            return await _context.TransactionTags
                .Where(tt => tt.TransactionId == transactionId)
                .Select(tt => tt.Tag)
                .ToListAsync();
        }

        public async Task<bool> IsAssignedToCardTransactionAsync(int tagId, int cardTransactionId)
        {
            return await _context.CardTransactionTags
                .AnyAsync(ct => ct.TagId == tagId && ct.CardTransactionId == cardTransactionId);
        }

        public async Task AssignToCardTransactionAsync(int tagId, int cardTransactionId)
        {
            if (await IsAssignedToCardTransactionAsync(tagId, cardTransactionId)) return;
            await _context.CardTransactionTags.AddAsync(new CardTransactionTag { TagId = tagId, CardTransactionId = cardTransactionId });
            await _context.SaveChangesAsync();
        }

        public async Task UnassignFromCardTransactionAsync(int tagId, int cardTransactionId)
        {
            var link = await _context.CardTransactionTags
                .FirstOrDefaultAsync(ct => ct.TagId == tagId && ct.CardTransactionId == cardTransactionId);
            if (link == null) return;
            _context.CardTransactionTags.Remove(link);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Tag>> GetTagsForCardTransactionAsync(int cardTransactionId)
        {
            return await _context.CardTransactionTags
                .Where(ct => ct.CardTransactionId == cardTransactionId)
                .Select(ct => ct.Tag)
                .ToListAsync();
        }
    }
}
