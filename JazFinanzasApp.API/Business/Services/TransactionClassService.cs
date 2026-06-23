using JazFinanzasApp.API.Business.DTO.TransactionClass;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class TransactionClassService : ITransactionClassService
    {
        private readonly ITransactionClassRepository _transactionClassRepository;

        public TransactionClassService(ITransactionClassRepository transactionClassRepository)
        {
            _transactionClassRepository = transactionClassRepository;
        }

        public async Task<IEnumerable<TransactionClassDTO>> GetAllForUserAsync(int userId)
        {
            var classes = await _transactionClassRepository.GetByUserIdAsync(userId);
            return classes.OrderBy(mc => mc.Description)
                .Select(mc => new TransactionClassDTO { Id = mc.Id, Description = mc.Description, IncExp = mc.IncExp, IsSystem = mc.IsSystem });
        }

        public async Task<TransactionClassDTO> GetByIdAsync(int userId, int id)
        {
            var tc = await _transactionClassRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction class not found");
            if (tc.UserId != userId) throw new UnauthorizedDomainException();
            return new TransactionClassDTO { Id = tc.Id, Description = tc.Description, IncExp = tc.IncExp, IsSystem = tc.IsSystem };
        }

        public async Task CreateTransactionClassAsync(int userId, TransactionClassDTO dto)
        {
            var checkExists = await _transactionClassRepository.FindAsync(mc => mc.Description == dto.Description && mc.UserId == userId);
            if (checkExists.Any()) throw new BusinessRuleException("Transaction Class already exists");
            await _transactionClassRepository.AddAsync(new TransactionClass
            {
                Description = dto.Description, IncExp = dto.IncExp, UserId = userId
            });
        }

        public async Task UpdateTransactionClassAsync(int userId, int id, TransactionClassDTO dto)
        {
            var tc = await _transactionClassRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction class not found");
            if (tc.UserId != userId) throw new UnauthorizedDomainException();
            if (tc.IsSystem) throw new BusinessRuleException("System transaction class cannot be edited");
            tc.Description = dto.Description;
            tc.UpdatedAt = DateTime.UtcNow;
            await _transactionClassRepository.UpdateAsync(tc);
        }

        public async Task DeleteTransactionClassAsync(int userId, int id)
        {
            var tc = await _transactionClassRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction class not found");
            if (tc.UserId != userId) throw new UnauthorizedDomainException();
            if (tc.IsSystem) throw new BusinessRuleException("System transaction class cannot be deleted");
            var isInUse = await _transactionClassRepository.IsTransactionClassInUseAsync(id);
            if (isInUse) throw new BusinessRuleException("Transaction Class is being used in transactions");
            await _transactionClassRepository.DeleteAsync(id);
        }
    }
}
