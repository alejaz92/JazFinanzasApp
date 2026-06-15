using JazFinanzasApp.API.Business.DTO.SharedExpense;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class SharedExpenseService : ISharedExpenseService
    {
        private readonly ISharedExpenseRepository _sharedExpenseRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IPersonRepository _personRepository;

        public SharedExpenseService(
            ISharedExpenseRepository sharedExpenseRepository,
            ITransactionRepository transactionRepository,
            IPersonRepository personRepository)
        {
            _sharedExpenseRepository = sharedExpenseRepository;
            _transactionRepository = transactionRepository;
            _personRepository = personRepository;
        }

        public async Task<SharedExpenseDetailDTO> CreateAsync(int userId, SharedExpenseAddDTO dto)
        {
            var transaction = await _transactionRepository.GetTransactionByIdAsync(dto.TransactionId)
                ?? throw new NotFoundException("Transacción no encontrada");

            if (transaction.UserId != userId)
                throw new UnauthorizedDomainException();

            if (transaction.MovementType != "E")
                throw new BusinessRuleException("Solo se pueden compartir transacciones de egreso");

            var existing = await _sharedExpenseRepository.GetByTransactionIdAsync(dto.TransactionId);
            if (existing != null)
                throw new BusinessRuleException("Esta transacción ya tiene un gasto compartido asociado");

            if (!dto.Splits.Any())
                throw new BusinessRuleException("Debe incluir al menos un split");

            var totalSplits = dto.Splits.Sum(s => s.Amount);
            if (totalSplits > Math.Abs(transaction.Amount))
                throw new BusinessRuleException("La suma de los splits no puede superar el monto de la transacción");

            foreach (var splitInput in dto.Splits)
            {
                var person = await _personRepository.GetByIdAsync(splitInput.PersonId)
                    ?? throw new NotFoundException($"Persona {splitInput.PersonId} no encontrada");
                if (person.UserId != userId)
                    throw new UnauthorizedDomainException();
            }

            var sharedExpense = new SharedExpense
            {
                TransactionId = dto.TransactionId,
                Notes = dto.Notes,
                UserId = userId,
                Splits = dto.Splits.Select(s => new SharedExpenseSplit
                {
                    PersonId = s.PersonId,
                    Amount = s.Amount,
                    AmountReimbursed = 0,
                    Status = SharedExpenseSplitStatus.Pending,
                    Notes = s.Notes
                }).ToList()
            };

            var created = await _sharedExpenseRepository.AddAsyncReturnObject(sharedExpense);

            var full = await _sharedExpenseRepository.GetByTransactionIdAsync(dto.TransactionId);
            return MapToDetailDTO(full!);
        }

        public async Task<SharedExpenseDetailDTO> GetByTransactionIdAsync(int userId, int transactionId)
        {
            var transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId)
                ?? throw new NotFoundException("Transacción no encontrada");

            if (transaction.UserId != userId)
                throw new UnauthorizedDomainException();

            var sharedExpense = await _sharedExpenseRepository.GetByTransactionIdAsync(transactionId)
                ?? throw new NotFoundException("Esta transacción no tiene un gasto compartido asociado");

            return MapToDetailDTO(sharedExpense);
        }

        public async Task DeleteAsync(int userId, int id)
        {
            var sharedExpense = await _sharedExpenseRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Gasto compartido no encontrado");

            if (sharedExpense.UserId != userId)
                throw new UnauthorizedDomainException();

            var full = await _sharedExpenseRepository.GetByTransactionIdAsync(sharedExpense.TransactionId);
            if (full!.Splits.Any(s => s.Status != SharedExpenseSplitStatus.Pending))
                throw new BusinessRuleException("No se puede eliminar un gasto compartido con splits que ya tienen reintegros");

            await _sharedExpenseRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<PersonDebtSummaryDTO>> GetSummaryAsync(int userId)
        {
            var splits = await _sharedExpenseRepository.GetPendingSplitsByUserIdAsync(userId);

            return splits
                .GroupBy(s => s.Person)
                .Select(g => new PersonDebtSummaryDTO
                {
                    PersonId = g.Key.Id,
                    PersonName = g.Key.Alias ?? g.Key.Name,
                    TotalPending = g.Sum(s => s.Amount - s.AmountReimbursed)
                })
                .OrderBy(x => x.PersonName)
                .ToList();
        }

        private static SharedExpenseDetailDTO MapToDetailDTO(SharedExpense se)
        {
            return new SharedExpenseDetailDTO
            {
                Id = se.Id,
                TransactionId = se.TransactionId,
                Notes = se.Notes,
                Splits = se.Splits.Select(s => new SharedExpenseSplitDTO
                {
                    Id = s.Id,
                    PersonId = s.PersonId,
                    PersonName = s.Person?.Alias ?? s.Person?.Name ?? string.Empty,
                    Amount = s.Amount,
                    AmountReimbursed = s.AmountReimbursed,
                    Status = s.Status,
                    Notes = s.Notes
                }).ToList()
            };
        }
    }
}
