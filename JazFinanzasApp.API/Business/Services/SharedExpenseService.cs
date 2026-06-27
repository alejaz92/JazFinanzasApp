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
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IPortfolioRepository _portfolioRepository;

        public SharedExpenseService(
            ISharedExpenseRepository sharedExpenseRepository,
            ITransactionRepository transactionRepository,
            IPersonRepository personRepository,
            ICardTransactionRepository cardTransactionRepository,
            IAccountRepository accountRepository,
            ITransactionClassRepository transactionClassRepository,
            IPortfolioRepository portfolioRepository)
        {
            _sharedExpenseRepository = sharedExpenseRepository;
            _transactionRepository = transactionRepository;
            _personRepository = personRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _accountRepository = accountRepository;
            _transactionClassRepository = transactionClassRepository;
            _portfolioRepository = portfolioRepository;
        }

        public async Task<SharedExpenseDetailDTO> CreateAsync(int userId, SharedExpenseAddDTO dto)
        {
            var hasTransaction = dto.TransactionId.HasValue;
            var hasCardTransaction = dto.CardTransactionId.HasValue;

            if (hasTransaction == hasCardTransaction)
                throw new BusinessRuleException("Debe informar exactamente uno de TransactionId o CardTransactionId");

            if (!dto.Splits.Any())
                throw new BusinessRuleException("Debe incluir al menos un split");

            return hasTransaction
                ? await CreateForAccountAsync(userId, dto)
                : await CreateForCardAsync(userId, dto);
        }

        private async Task<SharedExpenseDetailDTO> CreateForAccountAsync(int userId, SharedExpenseAddDTO dto)
        {
            var transaction = await _transactionRepository.GetTransactionByIdAsync(dto.TransactionId!.Value)
                ?? throw new NotFoundException("Transacción no encontrada");

            if (transaction.UserId != userId)
                throw new UnauthorizedDomainException();

            if (transaction.MovementType != "E")
                throw new BusinessRuleException("Solo se pueden compartir transacciones de egreso");

            var existing = await _sharedExpenseRepository.GetByTransactionIdAsync(dto.TransactionId.Value);
            if (existing != null)
                throw new BusinessRuleException("Esta transacción ya tiene un gasto compartido asociado");

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

            await _sharedExpenseRepository.AddAsyncReturnObject(sharedExpense);

            var full = await _sharedExpenseRepository.GetByTransactionIdAsync(dto.TransactionId.Value);
            return MapToDetailDTO(full!);
        }

        private async Task<SharedExpenseDetailDTO> CreateForCardAsync(int userId, SharedExpenseAddDTO dto)
        {
            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(dto.CardTransactionId!.Value)
                ?? throw new NotFoundException("Gasto de tarjeta no encontrado");

            if (cardTransaction.UserId != userId)
                throw new UnauthorizedDomainException();

            var existing = await _sharedExpenseRepository.GetByCardTransactionIdAsync(dto.CardTransactionId.Value);
            if (existing != null)
                throw new BusinessRuleException("Este gasto de tarjeta ya tiene un gasto compartido asociado");

            var totalSplits = dto.Splits.Sum(s => s.Amount);
            if (totalSplits > cardTransaction.TotalAmount)
                throw new BusinessRuleException("La suma de los splits no puede superar el monto del gasto de tarjeta");

            foreach (var splitInput in dto.Splits)
            {
                var person = await _personRepository.GetByIdAsync(splitInput.PersonId)
                    ?? throw new NotFoundException($"Persona {splitInput.PersonId} no encontrada");
                if (person.UserId != userId)
                    throw new UnauthorizedDomainException();
            }

            var splitEntities = dto.Splits.Select(s => new SharedExpenseSplit
            {
                PersonId = s.PersonId,
                Amount = s.Amount,
                AmountReimbursed = 0,
                AmountApplied = 0,
                InstallmentSplitAmount = Math.Round(s.Amount / cardTransaction.Installments, 2),
                Status = SharedExpenseSplitStatus.Pending,
                Notes = s.Notes
            }).ToList();

            var sharedExpense = new SharedExpense
            {
                CardTransactionId = dto.CardTransactionId,
                Notes = dto.Notes,
                UserId = userId,
                Splits = splitEntities
            };

            await _sharedExpenseRepository.AddAsyncReturnObject(sharedExpense);

            var full = await _sharedExpenseRepository.GetByCardTransactionIdAsync(dto.CardTransactionId.Value);
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

        public async Task<SharedExpenseDetailDTO> GetByCardTransactionIdAsync(int userId, int cardTransactionId)
        {
            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(cardTransactionId)
                ?? throw new NotFoundException("Gasto de tarjeta no encontrado");

            if (cardTransaction.UserId != userId)
                throw new UnauthorizedDomainException();

            var sharedExpense = await _sharedExpenseRepository.GetByCardTransactionIdAsync(cardTransactionId)
                ?? throw new NotFoundException("Este gasto de tarjeta no tiene un gasto compartido asociado");

            return MapToDetailDTO(sharedExpense);
        }

        public async Task<SharedExpenseSplitDTO> RegisterReimbursementAsync(int userId, RegisterReimbursementDTO dto)
        {
            var split = await _sharedExpenseRepository.GetSplitByIdAsync(dto.SplitId)
                ?? throw new NotFoundException("Split no encontrado");

            if (split.SharedExpense.UserId != userId)
                throw new UnauthorizedDomainException();

            if (split.SharedExpense.CardTransactionId == null)
                throw new BusinessRuleException("Este split no corresponde a un gasto de tarjeta");

            if (split.AmountReimbursed + dto.Amount > split.Amount)
                throw new BusinessRuleException("El monto del reintegro supera la deuda pendiente del split");

            var account = await _accountRepository.GetByIdAsync(dto.AccountId)
                ?? throw new NotFoundException("Cuenta no encontrada");
            if (account.UserId != userId)
                throw new UnauthorizedDomainException();

            var transactionClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Reintegro", userId)
                ?? throw new NotFoundException("Clase de transacción 'Reintegro' no encontrada");

            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(split.SharedExpense.CardTransactionId.Value)
                ?? throw new NotFoundException("Gasto de tarjeta no encontrado");

            var defaultPortfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                ?? throw new NotFoundException("Portfolio por defecto no encontrado");

            var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
            {
                AccountId = account.Id,
                Account = account,
                PortfolioId = defaultPortfolio.Id,
                Portfolio = defaultPortfolio,
                AssetId = cardTransaction.AssetId,
                Date = dto.Date,
                MovementType = "I",
                TransactionClassId = transactionClass.Id,
                TransactionClass = transactionClass,
                Detail = $"Reintegro - {cardTransaction.Detail}",
                Amount = dto.Amount,
                UserId = userId
            });

            await _sharedExpenseRepository.AddReimbursementAsync(new SharedExpenseReimbursement
            {
                SharedExpenseSplitId = split.Id,
                TransactionId = incomeTransaction.Id,
                Amount = dto.Amount,
                Date = dto.Date
            });

            split.AmountReimbursed += dto.Amount;
            split.Status = split.AmountReimbursed >= split.Amount
                ? SharedExpenseSplitStatus.Paid
                : SharedExpenseSplitStatus.PartiallyPaid;
            split.UpdatedAt = DateTime.UtcNow;
            await _sharedExpenseRepository.UpdateSplitAsync(split);

            return new SharedExpenseSplitDTO
            {
                Id = split.Id,
                PersonId = split.PersonId,
                PersonName = split.Person?.Alias ?? split.Person?.Name ?? string.Empty,
                Amount = split.Amount,
                AmountReimbursed = split.AmountReimbursed,
                AmountApplied = split.AmountApplied,
                InstallmentSplitAmount = split.InstallmentSplitAmount,
                Status = split.Status,
                Notes = split.Notes
            };
        }

        public async Task DeleteAsync(int userId, int id)
        {
            var sharedExpense = await _sharedExpenseRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Gasto compartido no encontrado");

            if (sharedExpense.UserId != userId)
                throw new UnauthorizedDomainException();

            var full = sharedExpense.TransactionId.HasValue
                ? await _sharedExpenseRepository.GetByTransactionIdAsync(sharedExpense.TransactionId.Value)
                : await _sharedExpenseRepository.GetByCardTransactionIdAsync(sharedExpense.CardTransactionId!.Value);

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
                    PersonId = g.Key!.Id,
                    PersonName = g.Key!.Alias ?? g.Key!.Name,
                    TotalPending = g.Sum(s => s.Amount - s.AmountReimbursed),
                    Splits = g.Select(s => new PersonDebtSplitDTO
                    {
                        SplitId = s.Id,
                        Description = s.SharedExpense.CardTransactionId.HasValue
                            ? (s.SharedExpense.CardTransaction?.Detail ?? "Gasto de tarjeta")
                            : (s.SharedExpense.Transaction?.Detail ?? "Gasto"),
                        Amount = s.Amount,
                        AmountReimbursed = s.AmountReimbursed,
                        Pending = Math.Round(s.Amount - s.AmountReimbursed, 2),
                        Status = s.Status,
                        CardTransactionId = s.SharedExpense.CardTransactionId,
                        TransactionId = s.SharedExpense.TransactionId
                    })
                    .OrderByDescending(s => s.Pending)
                    .ToList()
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
                CardTransactionId = se.CardTransactionId,
                Notes = se.Notes,
                Splits = se.Splits.Select(s => new SharedExpenseSplitDTO
                {
                    Id = s.Id,
                    PersonId = s.PersonId,
                    PersonName = s.Person?.Alias ?? s.Person?.Name ?? string.Empty,
                    Amount = s.Amount,
                    AmountReimbursed = s.AmountReimbursed,
                    AmountApplied = s.AmountApplied,
                    InstallmentSplitAmount = s.InstallmentSplitAmount,
                    Status = s.Status,
                    Notes = s.Notes
                }).ToList()
            };
        }
    }
}
