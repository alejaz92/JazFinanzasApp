using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
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

        public async Task<IEnumerable<Tag>> GetByUserIdAsync(int userId)
        {
            return await _context.Tags
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<int?> GetTransactionOwnerIdAsync(int transactionId)
        {
            return await _context.Transactions
                .Where(t => t.Id == transactionId)
                .Select(t => (int?)t.UserId)
                .FirstOrDefaultAsync();
        }

        public async Task<int?> GetCardTransactionOwnerIdAsync(int cardTransactionId)
        {
            return await _context.CardTransactions
                .Where(ct => ct.Id == cardTransactionId)
                .Select(ct => (int?)ct.UserId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsAssignedToTransactionAsync(int transactionId, int tagId)
        {
            return await _context.TransactionTags
                .AnyAsync(tt => tt.TransactionId == transactionId && tt.TagId == tagId);
        }

        public async Task<bool> IsAssignedToCardTransactionAsync(int cardTransactionId, int tagId)
        {
            return await _context.CardTransactionTags
                .AnyAsync(ctt => ctt.CardTransactionId == cardTransactionId && ctt.TagId == tagId);
        }

        public async Task AssignToTransactionAsync(int transactionId, int tagId)
        {
            await _context.TransactionTags.AddAsync(new TransactionTag { TransactionId = transactionId, TagId = tagId });
            await _context.SaveChangesAsync();
        }

        public async Task UnassignFromTransactionAsync(int transactionId, int tagId)
        {
            var link = await _context.TransactionTags
                .FirstOrDefaultAsync(tt => tt.TransactionId == transactionId && tt.TagId == tagId);
            if (link != null)
            {
                _context.TransactionTags.Remove(link);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AssignToCardTransactionAsync(int cardTransactionId, int tagId)
        {
            await _context.CardTransactionTags.AddAsync(new CardTransactionTag { CardTransactionId = cardTransactionId, TagId = tagId });
            await _context.SaveChangesAsync();
        }

        public async Task UnassignFromCardTransactionAsync(int cardTransactionId, int tagId)
        {
            var link = await _context.CardTransactionTags
                .FirstOrDefaultAsync(ctt => ctt.CardTransactionId == cardTransactionId && ctt.TagId == tagId);
            if (link != null)
            {
                _context.CardTransactionTags.Remove(link);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Tag>> GetTagsForTransactionAsync(int transactionId)
        {
            return await _context.TransactionTags
                .Where(tt => tt.TransactionId == transactionId)
                .Select(tt => tt.Tag)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tag>> GetTagsForCardTransactionAsync(int cardTransactionId)
        {
            return await _context.CardTransactionTags
                .Where(ctt => ctt.CardTransactionId == cardTransactionId)
                .Select(ctt => ctt.Tag)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task DeleteTagWithAssignmentsAsync(int tagId)
        {
            var transactionLinks = await _context.TransactionTags.Where(tt => tt.TagId == tagId).ToListAsync();
            var cardTransactionLinks = await _context.CardTransactionTags.Where(ctt => ctt.TagId == tagId).ToListAsync();
            _context.TransactionTags.RemoveRange(transactionLinks);
            _context.CardTransactionTags.RemoveRange(cardTransactionLinks);

            var tag = await _context.Tags.FindAsync(tagId);
            if (tag != null) _context.Tags.Remove(tag);

            await _context.SaveChangesAsync();
        }
    }
}
