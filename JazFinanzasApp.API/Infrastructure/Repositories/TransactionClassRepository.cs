using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
{
    public class TransactionClassRepository : GenericRepository<TransactionClass>, ITransactionClassRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionClassRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }
        public async Task<TransactionClass> GetTransactionClassByDescriptionAsync(string Description, int UserId)
        {
            return await _context.TransactionClasses
                .FirstOrDefaultAsync(m => m.Description == Description && m.UserId == UserId);
        }

        // check if a transaction class is being used in any transaction or card transaction
        public async Task<bool> IsTransactionClassInUseAsync(int transactionClassId)
        {
            return await _context.Transactions.AnyAsync(t => t.TransactionClassId == transactionClassId) ||
                   await _context.CardTransactions.AnyAsync(ct => ct.TransactionClassId == transactionClassId);
        }


    }
}
