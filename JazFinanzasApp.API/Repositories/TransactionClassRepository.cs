using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Report;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
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



    }
}
