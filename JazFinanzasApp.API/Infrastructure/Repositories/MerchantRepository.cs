using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class MerchantRepository : GenericRepository<Merchant>, IMerchantRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<MerchantAlias?> FindAliasAsync(int userId, string normalizedDetail)
        {
            return await _context.MerchantAliases
                .Include(a => a.Merchant)
                .FirstOrDefaultAsync(a => a.NormalizedDetail == normalizedDetail && a.Merchant.UserId == userId);
        }

        public async Task<Merchant> CreateMerchantWithAliasAsync(int userId, string name, string normalizedDetail)
        {
            var merchant = new Merchant { Name = name, UserId = userId, IsConfirmed = false };
            await _context.Merchants.AddAsync(merchant);
            await _context.SaveChangesAsync(); // necesita el Id generado antes de crear el alias

            await _context.MerchantAliases.AddAsync(new MerchantAlias
            {
                MerchantId = merchant.Id,
                NormalizedDetail = normalizedDetail,
                IsManual = false
            });
            await _context.SaveChangesAsync();

            return merchant;
        }

        public async Task<IEnumerable<Merchant>> GetByUserIdAsync(int userId)
        {
            return await _context.Merchants
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetVolumesByMerchantAsync(int userId)
        {
            var txCounts = await _context.Transactions
                .Where(t => t.UserId == userId && t.MerchantId != null)
                .GroupBy(t => t.MerchantId!.Value)
                .Select(g => new { MerchantId = g.Key, Count = g.Count() })
                .ToListAsync();

            var ctCounts = await _context.CardTransactions
                .Where(ct => ct.UserId == userId && ct.MerchantId != null)
                .GroupBy(ct => ct.MerchantId!.Value)
                .Select(g => new { MerchantId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<int, int>();
            foreach (var tc in txCounts) result[tc.MerchantId] = result.GetValueOrDefault(tc.MerchantId) + tc.Count;
            foreach (var cc in ctCounts) result[cc.MerchantId] = result.GetValueOrDefault(cc.MerchantId) + cc.Count;
            return result;
        }

        // Solo egresos que no sean cuota de tarjeta: un comercio es la contraparte de un gasto, y
        // el gasto de tarjeta vive en el CardTransaction, no en sus cuotas (ver MerchantEligibility).
        public async Task<IEnumerable<Transaction>> GetUnresolvedTransactionsAsync(int userId)
        {
            return await _context.Transactions
                .Where(t => t.UserId == userId
                            && t.MerchantId == null
                            && t.MovementType == "E"
                            && t.CardTransactionId == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<CardTransaction>> GetUnresolvedCardTransactionsAsync(int userId)
        {
            return await _context.CardTransactions
                .Where(ct => ct.UserId == userId && ct.MerchantId == null)
                .ToListAsync();
        }

        public async Task SetTransactionMerchantAsync(int transactionId, int? merchantId)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null) return;
            transaction.MerchantId = merchantId;
            await _context.SaveChangesAsync();
        }

        public async Task SetCardTransactionMerchantAsync(int cardTransactionId, int? merchantId)
        {
            var cardTransaction = await _context.CardTransactions.FindAsync(cardTransactionId);
            if (cardTransaction == null) return;
            cardTransaction.MerchantId = merchantId;
            await _context.SaveChangesAsync();
        }

        public async Task UpsertManualAliasAsync(int merchantId, string normalizedDetail)
        {
            var merchant = await _context.Merchants.FindAsync(merchantId);
            if (merchant == null) return;

            // Busca entre TODOS los alias del usuario, no solo los de este merchant — el usuario
            // puede estar corrigiendo una asignación que antes apuntaba a otro comercio.
            var existing = await _context.MerchantAliases
                .Include(a => a.Merchant)
                .FirstOrDefaultAsync(a => a.NormalizedDetail == normalizedDetail && a.Merchant.UserId == merchant.UserId);

            if (existing != null)
            {
                existing.MerchantId = merchantId;
                existing.IsManual = true;
            }
            else
            {
                await _context.MerchantAliases.AddAsync(new MerchantAlias
                {
                    MerchantId = merchantId,
                    NormalizedDetail = normalizedDetail,
                    IsManual = true
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task MergeAsync(int sourceMerchantId, int targetMerchantId)
        {
            var transactions = await _context.Transactions.Where(t => t.MerchantId == sourceMerchantId).ToListAsync();
            foreach (var t in transactions) t.MerchantId = targetMerchantId;

            var cardTransactions = await _context.CardTransactions.Where(ct => ct.MerchantId == sourceMerchantId).ToListAsync();
            foreach (var ct in cardTransactions) ct.MerchantId = targetMerchantId;

            var targetAliasTexts = await _context.MerchantAliases
                .Where(a => a.MerchantId == targetMerchantId)
                .Select(a => a.NormalizedDetail)
                .ToListAsync();

            var sourceAliases = await _context.MerchantAliases.Where(a => a.MerchantId == sourceMerchantId).ToListAsync();
            foreach (var alias in sourceAliases)
            {
                if (targetAliasTexts.Contains(alias.NormalizedDetail))
                {
                    // el destino ya cubre este texto — descartarlo en vez de duplicarlo
                    _context.MerchantAliases.Remove(alias);
                }
                else
                {
                    alias.MerchantId = targetMerchantId;
                }
            }

            var source = await _context.Merchants.FindAsync(sourceMerchantId);
            if (source != null) _context.Merchants.Remove(source);

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByMerchantAsync(int merchantId)
        {
            return await _context.Transactions
                .Where(t => t.MerchantId == merchantId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<CardTransaction>> GetCardTransactionsByMerchantAsync(int merchantId)
        {
            return await _context.CardTransactions
                .Where(ct => ct.MerchantId == merchantId)
                .OrderByDescending(ct => ct.Date)
                .ToListAsync();
        }
    }
}
