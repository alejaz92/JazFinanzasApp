using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class CardTransactionDiscountRepository : GenericRepository<CardTransactionDiscount>, ICardTransactionDiscountRepository
    {
        private readonly ApplicationDbContext _context;

        public CardTransactionDiscountRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<CardTransactionDiscount?> GetByCardTransactionIdAsync(int cardTransactionId)
        {
            return await _context.CardTransactionDiscounts
                .FirstOrDefaultAsync(d => d.CardTransactionId == cardTransactionId);
        }

        public async Task AddInstallmentAsync(CardTransactionDiscountInstallment installment)
        {
            await _context.CardTransactionDiscountInstallments.AddAsync(installment);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<CardTransactionDiscountInstallment>> GetInstallmentsByDiscountIdAsync(int discountId)
        {
            return await _context.CardTransactionDiscountInstallments
                .Where(i => i.CardTransactionDiscountId == discountId)
                .OrderBy(i => i.Id)
                .ToListAsync();
        }

        public async Task DeleteInstallmentAsync(int id)
        {
            var installment = await _context.CardTransactionDiscountInstallments.FindAsync(id);
            if (installment != null)
            {
                _context.CardTransactionDiscountInstallments.Remove(installment);
                await _context.SaveChangesAsync();
            }
        }
    }
}
