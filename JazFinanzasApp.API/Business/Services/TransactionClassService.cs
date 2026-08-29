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
            return classes.OrderBy(mc => mc.Description).Select(ToDTO);
        }

        public async Task<TransactionClassDTO> GetByIdAsync(int userId, int id)
        {
            var tc = await _transactionClassRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction class not found");
            if (tc.UserId != userId) throw new UnauthorizedDomainException();
            return ToDTO(tc);
        }

        public async Task CreateTransactionClassAsync(int userId, TransactionClassDTO dto)
        {
            var checkExists = await _transactionClassRepository.FindAsync(mc => mc.Description == dto.Description && mc.UserId == userId);
            if (checkExists.Any()) throw new BusinessRuleException("Transaction Class already exists");
            ValidateNature(dto.Nature);
            await ValidateHierarchyAsync(userId, id: null, dto.ParentId);
            await _transactionClassRepository.AddAsync(new TransactionClass
            {
                Description = dto.Description, IncExp = dto.IncExp, UserId = userId,
                ParentId = dto.ParentId, Nature = dto.Nature
            });
        }

        public async Task UpdateTransactionClassAsync(int userId, int id, TransactionClassDTO dto)
        {
            var tc = await _transactionClassRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction class not found");
            if (tc.UserId != userId) throw new UnauthorizedDomainException();
            if (tc.IsSystem) throw new BusinessRuleException("System transaction class cannot be edited");
            ValidateNature(dto.Nature);
            await ValidateHierarchyAsync(userId, id, dto.ParentId);
            tc.Description = dto.Description;
            tc.ParentId = dto.ParentId;
            tc.Nature = dto.Nature;
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

        private static void ValidateNature(string? nature)
        {
            if (nature != null && !TransactionClassNature.IsValid(nature))
                throw new BusinessRuleException("Invalid transaction class nature");
        }

        // T13 (plan-rediseno-reportes.md): jerarquía de un solo nivel. Tres guardas:
        // (1) una categoría no puede ser su propio padre, (2) el padre elegido no puede a su
        // vez tener padre, (3) una categoría que ya tiene hijos no puede pasar a tener padre
        // (eso los convertiría en nietos, un tercer nivel).
        private async Task ValidateHierarchyAsync(int userId, int? id, int? parentId)
        {
            if (parentId == null) return;

            if (parentId == id)
                throw new BusinessRuleException("A transaction class cannot be its own parent");

            var parent = await _transactionClassRepository.GetByIdAsync(parentId.Value)
                ?? throw new NotFoundException("Parent transaction class not found");
            if (parent.UserId != userId) throw new UnauthorizedDomainException();
            if (parent.ParentId != null)
                throw new BusinessRuleException("A parent transaction class cannot itself have a parent");

            if (id != null)
            {
                var children = await _transactionClassRepository.FindAsync(c => c.ParentId == id);
                if (children.Any())
                    throw new BusinessRuleException("A transaction class with subcategories cannot become a child itself");
            }
        }

        private static TransactionClassDTO ToDTO(TransactionClass tc) => new TransactionClassDTO
        {
            Id = tc.Id, Description = tc.Description, IncExp = tc.IncExp, IsSystem = tc.IsSystem,
            ParentId = tc.ParentId, Nature = tc.Nature
        };
    }
}
