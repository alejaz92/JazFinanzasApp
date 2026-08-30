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
                .Select(ToDto);
        }

        public async Task<TransactionClassDTO> GetByIdAsync(int userId, int id)
        {
            var tc = await _transactionClassRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction class not found");
            if (tc.UserId != userId) throw new UnauthorizedDomainException();
            return ToDto(tc);
        }

        public async Task CreateTransactionClassAsync(int userId, TransactionClassDTO dto)
        {
            var checkExists = await _transactionClassRepository.FindAsync(mc => mc.Description == dto.Description && mc.UserId == userId);
            if (checkExists.Any()) throw new BusinessRuleException("Transaction Class already exists");
            await ValidateParentAsync(userId, dto.ParentId, currentId: null);
            await _transactionClassRepository.AddAsync(new TransactionClass
            {
                Description = dto.Description,
                IncExp = dto.IncExp,
                UserId = userId,
                CountsAsIncomeExpense = dto.CountsAsIncomeExpense,
                ParentId = dto.ParentId
            });
        }

        public async Task UpdateTransactionClassAsync(int userId, int id, TransactionClassDTO dto)
        {
            var tc = await _transactionClassRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction class not found");
            if (tc.UserId != userId) throw new UnauthorizedDomainException();
            if (tc.IsSystem) throw new BusinessRuleException("System transaction class cannot be edited");
            await ValidateParentAsync(userId, dto.ParentId, currentId: id);
            if (dto.ParentId != null && await _transactionClassRepository.HasChildrenAsync(id))
                throw new BusinessRuleException("A category with subcategories cannot become a subcategory itself");
            tc.Description = dto.Description;
            tc.CountsAsIncomeExpense = dto.CountsAsIncomeExpense;
            tc.ParentId = dto.ParentId;
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
            var hasChildren = await _transactionClassRepository.HasChildrenAsync(id);
            if (hasChildren) throw new BusinessRuleException("Transaction Class has subcategories");
            await _transactionClassRepository.DeleteAsync(id);
        }

        // Máximo dos niveles: el rubro elegido como padre no puede tener a su vez un padre.
        private async Task ValidateParentAsync(int userId, int? parentId, int? currentId)
        {
            if (parentId == null) return;
            if (parentId == currentId) throw new BusinessRuleException("A category cannot be its own parent");

            var parent = await _transactionClassRepository.GetByIdAsync(parentId.Value)
                ?? throw new BusinessRuleException("Parent category not found");
            if (parent.UserId != userId) throw new UnauthorizedDomainException();
            if (parent.ParentId != null) throw new BusinessRuleException("A subcategory cannot be used as a parent (maximum two levels)");
        }

        private static TransactionClassDTO ToDto(TransactionClass tc) => new TransactionClassDTO
        {
            Id = tc.Id,
            Description = tc.Description,
            IncExp = tc.IncExp,
            IsSystem = tc.IsSystem,
            CountsAsIncomeExpense = tc.CountsAsIncomeExpense,
            ParentId = tc.ParentId
        };
    }
}
