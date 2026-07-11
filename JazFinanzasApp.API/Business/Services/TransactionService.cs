using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;
using JazFinanzasApp.API.Business.DTO.Transaction;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IInvestmentTransactionRepository _investmentTransactionRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISharedExpenseRepository _sharedExpenseRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ITripSuggestionDismissalRepository _tripSuggestionDismissalRepository;
        private readonly ISharedEventMovementRepository _sharedEventMovementRepository;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            ITransactionClassRepository transactionClassRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IInvestmentTransactionRepository investmentTransactionRepository,
            IPortfolioRepository portfolioRepository,
            IUnitOfWork unitOfWork,
            ISharedExpenseRepository sharedExpenseRepository,
            ITripRepository tripRepository,
            ITripSuggestionDismissalRepository tripSuggestionDismissalRepository,
            ISharedEventMovementRepository sharedEventMovementRepository)
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _transactionClassRepository = transactionClassRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _investmentTransactionRepository = investmentTransactionRepository;
            _portfolioRepository = portfolioRepository;
            _unitOfWork = unitOfWork;
            _sharedExpenseRepository = sharedExpenseRepository;
            _tripRepository = tripRepository;
            _tripSuggestionDismissalRepository = tripSuggestionDismissalRepository;
            _sharedEventMovementRepository = sharedEventMovementRepository;
        }

        public async Task<(IEnumerable<TransactionListDTO> Transactions, int TotalCount)> GetPaginatedTransactionsAsync(int userId, int page, int pageSize)
        {
            var (transactions, totalCount) = await _transactionRepository.GetPaginatedTransactions(userId, page, pageSize);

            var transactionsDTO = transactions.Select(m => new TransactionListDTO
            {
                Id = m.Id,
                Date = m.Date,
                Amount = m.Amount,
                Detail = m.Detail,
                AccountId = m.AccountId,
                AccountName = m.Account.Name,
                PortfolioId = m.PortfolioId,
                PortfolioName = m.Portfolio.Name,
                AssetId = m.AssetId,
                AssetName = m.Asset.Name,
                AssetSymbol = m.Asset.Symbol,
                TransactionClassId = m.TransactionClassId,
                TransactionClassName = m.TransactionClass.Description,
                MovementType = m.MovementType,
                TripId = m.TripId,
                TripName = m.Trip?.Name
            }).ToList();

            return (transactionsDTO, totalCount);
        }

        public async Task<TransactionListDTO> GetTransactionByIdAsync(int userId, int id)
        {
            var transaction = await _transactionRepository.GetTransactionByIdAsync(id)
                ?? throw new NotFoundException("Transaction not found");

            if (transaction.UserId != userId)
                throw new UnauthorizedDomainException();

            return new TransactionListDTO
            {
                Id = transaction.Id,
                Date = transaction.Date,
                Amount = transaction.Amount,
                Detail = transaction.Detail,
                AccountId = transaction.AccountId,
                AccountName = transaction.Account.Name,
                PortfolioId = transaction.PortfolioId,
                PortfolioName = transaction.Portfolio.Name,
                AssetId = transaction.AssetId,
                AssetName = transaction.Asset.Name,
                AssetSymbol = transaction.Asset.Symbol,
                TransactionClassId = transaction.TransactionClassId,
                TransactionClassName = transaction.TransactionClass.Description,
                MovementType = transaction.MovementType,
                TripId = transaction.TripId,
                TripName = transaction.Trip?.Name
            };
        }

        public async Task<int> CreateTransactionAsync(int userId, TransactionAddDTO transactionDTO)
        {
            if (transactionDTO.tripId != null && transactionDTO.movementType != "E")
                throw new BusinessRuleException("Solo un egreso puede asociarse a un viaje");

            var defaultPortfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                ?? throw new NotFoundException("Default portfolio not found");

            var asset = await _assetRepository.GetByIdAsync(transactionDTO.assetId)
                ?? throw new NotFoundException("Asset not found");

            decimal quotePrice = await ResolveQuotePriceAsync(asset, transactionDTO.quotePrice, transactionDTO.date);

            if (transactionDTO.movementType == "I")
            {
                var incomeAccount = await _accountRepository.GetByIdAsync(transactionDTO.incomeAccountId.Value)
                    ?? throw new NotFoundException("Account not found");
                if (incomeAccount.UserId != userId) throw new UnauthorizedDomainException();

                var transactionClass = await _transactionClassRepository.GetByIdAsync(transactionDTO.transactionClassId.Value)
                    ?? throw new NotFoundException("Transaction class not found");
                if (transactionClass.IncExp == "E")
                    throw new BusinessRuleException("No se puede asignar una clase de movimiento de tipo egreso a un movimiento de tipo ingreso");
                if (transactionClass.UserId != userId) throw new UnauthorizedDomainException();

                var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = incomeAccount.Id,
                    Account = incomeAccount,
                    PortfolioId = defaultPortfolio.Id,
                    Portfolio = defaultPortfolio,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = transactionDTO.date,
                    MovementType = transactionDTO.movementType,
                    TransactionClassId = transactionClass.Id,
                    TransactionClass = transactionClass,
                    Detail = transactionDTO.detail,
                    Amount = transactionDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice
                });
                return incomeTransaction.Id;
            }
            else if (transactionDTO.movementType == "E")
            {
                var expenseAccount = await _accountRepository.GetByIdAsync(transactionDTO.expenseAccountId.Value)
                    ?? throw new NotFoundException("Account not found");
                if (expenseAccount.UserId != userId) throw new UnauthorizedDomainException();

                var transactionClass = await _transactionClassRepository.GetByIdAsync(transactionDTO.transactionClassId.Value)
                    ?? throw new NotFoundException("Transaction class not found");
                if (transactionClass.IncExp == "I")
                    throw new BusinessRuleException("No se puede asignar una clase de movimiento de tipo ingreso a un movimiento de tipo egreso");
                if (transactionClass.UserId != userId) throw new UnauthorizedDomainException();

                var balance = await _transactionRepository.GetBalance(transactionDTO.expenseAccountId.Value, asset.Id, defaultPortfolio.Id);
                if (balance < transactionDTO.amount)
                    throw new BusinessRuleException("No hay suficiente saldo en la cuenta");

                if (transactionDTO.tripId != null)
                    await ValidateTripAssignmentAsync(userId, transactionDTO.tripId.Value, transactionClass);

                var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = expenseAccount.Id,
                    Account = expenseAccount,
                    PortfolioId = defaultPortfolio.Id,
                    Portfolio = defaultPortfolio,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = transactionDTO.date,
                    MovementType = transactionDTO.movementType,
                    TransactionClassId = transactionClass.Id,
                    TransactionClass = transactionClass,
                    Detail = transactionDTO.detail,
                    Amount = -transactionDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice,
                    TripId = transactionDTO.tripId
                });
                return expenseTransaction.Id;
            }
            else if (transactionDTO.movementType == "EX")
            {
                var time = DateTime.UtcNow;

                var incomeAccount = await _accountRepository.GetByIdAsync(transactionDTO.incomeAccountId.Value)
                    ?? throw new NotFoundException("Income account not found");
                if (incomeAccount.UserId != userId) throw new UnauthorizedDomainException();

                var expenseAccount = await _accountRepository.GetByIdAsync(transactionDTO.expenseAccountId.Value)
                    ?? throw new NotFoundException("Expense account not found");
                if (expenseAccount.UserId != userId) throw new UnauthorizedDomainException();

                var balance = await _transactionRepository.GetBalance(transactionDTO.expenseAccountId.Value, asset.Id, defaultPortfolio.Id);
                if (balance < transactionDTO.amount)
                    throw new BusinessRuleException("No hay suficiente saldo en la cuenta");

                var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = incomeAccount.Id,
                    Account = incomeAccount,
                    PortfolioId = defaultPortfolio.Id,
                    Portfolio = defaultPortfolio,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = transactionDTO.date,
                    MovementType = transactionDTO.movementType,
                    TransactionClassId = null,
                    Detail = transactionDTO.detail,
                    Amount = transactionDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice,
                    CreatedAt = time,
                    UpdatedAt = time
                });

                var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = expenseAccount.Id,
                    Account = expenseAccount,
                    PortfolioId = defaultPortfolio.Id,
                    Portfolio = defaultPortfolio,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = transactionDTO.date,
                    MovementType = transactionDTO.movementType,
                    TransactionClassId = null,
                    Detail = transactionDTO.detail,
                    Amount = -transactionDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice,
                    CreatedAt = time,
                    UpdatedAt = time
                });

                await _investmentTransactionRepository.AddAsync(new InvestmentTransaction
                {
                    Date = transactionDTO.date,
                    Environment = "Exchange",
                    MovementType = "EX",
                    CommerceType = "Exchange",
                    IncomeTransactionId = incomeTransaction.Id,
                    ExpenseTransactionId = expenseTransaction.Id,
                    UserId = userId
                });
            }
            return 0;
        }

        public async Task EditTransactionAsync(int userId, int id, TransactionEditDTO transactionDTO)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();

            if (await _sharedEventMovementRepository.IsTransactionReferencedAsync(id))
                throw new BusinessRuleException("Esta transacción pertenece a un evento compartido; se edita desde el evento");

            if (transaction.TransactionClassId != transactionDTO.TransactionClassId)
            {
                var transactionClass = await _transactionClassRepository.GetByIdAsync(transactionDTO.TransactionClassId)
                    ?? throw new NotFoundException("Transaction class not found");
                if (transactionClass.UserId != userId) throw new UnauthorizedDomainException();
                transaction.TransactionClassId = transactionDTO.TransactionClassId;
            }

            if (transaction.AccountId != transactionDTO.AccountID)
            {
                var account = await _accountRepository.GetByIdAsync(transactionDTO.AccountID)
                    ?? throw new NotFoundException("Account not found");
                if (account.UserId != userId) throw new UnauthorizedDomainException();
                transaction.AccountId = transactionDTO.AccountID;
            }

            if (transaction.Date != transactionDTO.Date || transaction.AssetId != transactionDTO.AssetId)
            {
                var asset = await _assetRepository.GetByIdAsync(transactionDTO.AssetId);
                string type = asset.Symbol == "ARS" ? "BLUE" : "NA";
                decimal quotePrice = asset.Symbol == "USD" ? 1 : await _assetQuoteRepository.GetQuotePrice(asset.Id, transactionDTO.Date, type);
                transaction.QuotePrice = quotePrice;
                transaction.Date = transactionDTO.Date;
                transaction.AssetId = transactionDTO.AssetId;
            }

            if (transaction.TripId != transactionDTO.TripId)
            {
                if (transactionDTO.TripId != null)
                {
                    if (transaction.MovementType != "E")
                        throw new BusinessRuleException("Solo un egreso puede asociarse a un viaje");
                    if (transaction.CardTransactionId != null
                        || (transaction.Detail != null && transaction.Detail.StartsWith(TripMovementRules.LegacyCardPaymentDetailPrefix)))
                        throw new BusinessRuleException("Los pagos de cuotas de tarjeta no se asocian a viajes: el consumo de tarjeta se asocia directo");

                    var newClass = await _transactionClassRepository.GetByIdAsync(transaction.TransactionClassId.Value)
                        ?? throw new NotFoundException("Transaction class not found");
                    await ValidateTripAssignmentAsync(userId, transactionDTO.TripId.Value, newClass);
                }
                transaction.TripId = transactionDTO.TripId;
            }

            transaction.Amount = (transaction.MovementType == "E" && transactionDTO.Amount > 0)
                ? -transactionDTO.Amount
                : transactionDTO.Amount;
            transaction.Detail = transactionDTO.Detail;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(transaction);
        }

        public async Task DeleteTransactionAsync(int userId, int id)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();

            if (await _sharedEventMovementRepository.IsTransactionReferencedAsync(id))
                throw new BusinessRuleException("Esta transacción pertenece a un evento compartido; se elimina desde el evento");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _sharedExpenseRepository.DeleteByTransactionIdAsync(id);
                await _tripSuggestionDismissalRepository.DeleteByTransactionIdAsync(id);
                await _transactionRepository.DeleteAsync(transaction.Id);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RefundTransactionAsync(int userId, int id, RefundDTO refundDTO)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();
            if (transaction.MovementType == "I")
                throw new BusinessRuleException("Cannot refund an income transaction");

            var refundAccount = await _accountRepository.GetByIdAsync(refundDTO.AccountId)
                ?? throw new NotFoundException("Account not found");

            transaction.Amount = transaction.Amount + refundDTO.Amount;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction);

            if (transaction.AccountId != refundAccount.Id)
            {
                var time = DateTime.UtcNow;

                await _transactionRepository.AddAsync(new Transaction
                {
                    AccountId = transaction.AccountId,
                    Account = transaction.Account,
                    PortfolioId = transaction.PortfolioId,
                    Portfolio = transaction.Portfolio,
                    AssetId = transaction.AssetId,
                    Asset = transaction.Asset,
                    Date = refundDTO.Date,
                    MovementType = "EX",
                    TransactionClassId = null,
                    Detail = "Refund",
                    Amount = -refundDTO.Amount,
                    UserId = userId,
                    CreatedAt = time,
                    UpdatedAt = time,
                    QuotePrice = transaction.QuotePrice
                });

                await _transactionRepository.AddAsync(new Transaction
                {
                    AccountId = refundAccount.Id,
                    Account = refundAccount,
                    PortfolioId = transaction.PortfolioId,
                    Portfolio = transaction.Portfolio,
                    AssetId = transaction.AssetId,
                    Asset = transaction.Asset,
                    Date = refundDTO.Date,
                    MovementType = "EX",
                    TransactionClassId = null,
                    Detail = "Refund",
                    Amount = refundDTO.Amount,
                    UserId = userId,
                    CreatedAt = time,
                    UpdatedAt = time,
                    QuotePrice = transaction.QuotePrice
                });
            }

            if (refundDTO.SplitAllocations != null && refundDTO.SplitAllocations.Any())
            {
                var sharedExpense = await _sharedExpenseRepository.GetByTransactionIdAsync(id)
                    ?? throw new BusinessRuleException("Esta transacción no tiene un gasto compartido asociado");

                var validSplitIds = sharedExpense.Splits.ToDictionary(s => s.Id);

                foreach (var allocation in refundDTO.SplitAllocations)
                {
                    if (!validSplitIds.ContainsKey(allocation.SplitId))
                        throw new BusinessRuleException($"El split {allocation.SplitId} no pertenece a esta transacción");
                }

                var allocationsSum = refundDTO.SplitAllocations.Sum(a => a.Amount);
                if (allocationsSum != refundDTO.Amount)
                    throw new BusinessRuleException("La suma de las allocations debe ser igual al monto del reintegro");

                foreach (var allocation in refundDTO.SplitAllocations)
                {
                    var split = validSplitIds[allocation.SplitId];

                    if (split.AmountReimbursed + allocation.Amount > split.Amount)
                        throw new BusinessRuleException($"El monto asignado al split {split.Id} supera la deuda original");

                    split.AmountReimbursed += allocation.Amount;
                    split.Status = split.AmountReimbursed >= split.Amount
                        ? SharedExpenseSplitStatus.Paid
                        : SharedExpenseSplitStatus.PartiallyPaid;
                    split.UpdatedAt = DateTime.UtcNow;

                    await _sharedExpenseRepository.UpdateSplitAsync(split);
                }
            }
        }

        public async Task<(IEnumerable<CurrencyExchangeListDTO> Transactions, int TotalCount)> GetPaginatedExchangeTransactionsAsync(int userId, int page, int pageSize)
        {
            var (transactions, totalCount) = await _investmentTransactionRepository.GetPaginatedInvestmentTransactions(userId, page, pageSize, "Exchange");

            var transactionsDTO = transactions.Select(m => new CurrencyExchangeListDTO
            {
                Id = m.Id,
                Date = m.Date,
                ExpenseAsset = m.ExpenseTransaction.Asset.Name,
                ExpenseAccount = m.ExpenseTransaction.Account.Name,
                ExpenseAmount = m.ExpenseTransaction.Amount,
                IncomeAsset = m.IncomeTransaction.Asset.Name,
                IncomeAccount = m.IncomeTransaction.Account.Name,
                IncomeAmount = m.IncomeTransaction.Amount
            }).ToList();

            return (transactionsDTO, totalCount);
        }

        public async Task DeleteExchangeTransactionAsync(int userId, int id)
        {
            var investmentTransaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id)
                ?? throw new NotFoundException("Exchange transaction not found");
            if (investmentTransaction.UserId != userId) throw new UnauthorizedDomainException();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (investmentTransaction.IncomeTransaction != null)
                    await _transactionRepository.DeleteAsync(investmentTransaction.IncomeTransactionId.Value);
                if (investmentTransaction.ExpenseTransaction != null)
                    await _transactionRepository.DeleteAsync(investmentTransaction.ExpenseTransactionId.Value);
                await _investmentTransactionRepository.DeleteAsync(investmentTransaction.Id);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task ValidateTripAssignmentAsync(int userId, int tripId, TransactionClass transactionClass)
        {
            var trip = await _tripRepository.GetByIdAsync(tripId)
                ?? throw new NotFoundException("Trip not found");
            if (trip.UserId != userId) throw new UnauthorizedDomainException();

            if (TripMovementRules.ExcludedTransactionClasses.Contains(transactionClass.Description))
                throw new BusinessRuleException("La clase del movimiento no es asociable a un viaje");
        }

        private async Task<decimal> ResolveQuotePriceAsync(Asset asset, decimal quotePrice, DateTime date)
        {
            if (asset.Symbol == "USD") return 1;
            if (quotePrice != 0)
            {
                if (asset.Symbol == "ARS") return quotePrice;
                return 1 / quotePrice;
            }
            string type = asset.Symbol == "ARS" ? "BLUE" : "NA";
            return await _assetQuoteRepository.GetQuotePrice(asset.Id, date, type);
        }
    }
}
