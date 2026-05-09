using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class InvestmentTransactionService : IInvestmentTransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IInvestmentTransactionRepository _investmentTransactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IUnitOfWork _unitOfWork;

        public InvestmentTransactionService(
            ITransactionRepository transactionRepository,
            IInvestmentTransactionRepository investmentTransactionRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            IAssetQuoteRepository assetQuoteRepository,
            ITransactionClassRepository transactionClassRepository,
            IPortfolioRepository portfolioRepository,
            IUnitOfWork unitOfWork)
        {
            _transactionRepository = transactionRepository;
            _investmentTransactionRepository = investmentTransactionRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _transactionClassRepository = transactionClassRepository;
            _portfolioRepository = portfolioRepository;
            _unitOfWork = unitOfWork;
        }

        // ── Stocks ────────────────────────────────────────────────────────────

        public async Task CreateStockTransactionAsync(int userId, StockTransactionAddDTO dto)
        {
            if (dto.Environment != "Stock") throw new BusinessRuleException("Invalid environment");

            var incomeId = 0;
            var expenseId = 0;

            if (dto.StockMovementType == "I")
            {
                var incomeAsset = await _assetRepository.GetByIdAsync(dto.IncomeAssetId.Value)
                    ?? throw new NotFoundException("Income asset not found");
                var incomeAccount = await _accountRepository.GetByIdAsync(dto.IncomeAccountId.Value)
                    ?? throw new NotFoundException("Income account not found");
                var incomePortfolio = await _portfolioRepository.GetByIdAsync(dto.IncomePortfolioID.Value)
                    ?? throw new NotFoundException("Income portfolio not found");

                var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = dto.IncomeAccountId.Value,
                    Account = incomeAccount,
                    PortfolioId = incomePortfolio.Id,
                    Portfolio = incomePortfolio,
                    AssetId = dto.IncomeAssetId.Value,
                    Asset = incomeAsset,
                    Date = dto.Date,
                    MovementType = "I",
                    TransactionClassId = null,
                    Detail = dto.CommerceType,
                    Amount = dto.IncomeQuantity.Value,
                    QuotePrice = 1 / dto.IncomeQuotePrice.Value,
                    UserId = userId
                });
                incomeId = incomeTransaction.Id;

                if (dto.CommerceType == "General")
                {
                    var expenseAsset = await _assetRepository.GetByIdAsync(dto.ExpenseAssetId.Value)
                        ?? throw new NotFoundException("Expense asset not found");
                    var expenseAccount = await _accountRepository.GetByIdAsync(dto.ExpenseAccountId.Value)
                        ?? throw new NotFoundException("Expense account not found");
                    var expensePortfolio = await _portfolioRepository.GetByIdAsync(dto.ExpensePortfolioID.Value)
                        ?? throw new NotFoundException("Expense portfolio not found");

                    var balance = await _transactionRepository.GetBalance(dto.ExpenseAccountId.Value, dto.ExpenseAssetId.Value, dto.ExpensePortfolioID.Value);
                    if (balance < dto.ExpenseQuantity.Value)
                        throw new BusinessRuleException("Not enough balance in the account");

                    var investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Inversiones", userId)
                        ?? throw new BusinessRuleException("Transaction class 'Inversiones' not found");

                    string? assetQuoteType = expenseAsset.Name == "Peso Argentino" ? "BLUE" : null;
                    var quote = await _assetQuoteRepository.GetLastQuoteByAsset(expenseAsset.Id, assetQuoteType);

                    var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                    {
                        AccountId = dto.ExpenseAccountId.Value,
                        Account = expenseAccount,
                        PortfolioId = expensePortfolio.Id,
                        Portfolio = expensePortfolio,
                        AssetId = dto.ExpenseAssetId.Value,
                        Asset = expenseAsset,
                        Date = dto.Date,
                        MovementType = "E",
                        TransactionClassId = investmentClass.Id,
                        Detail = dto.CommerceType,
                        Amount = -dto.ExpenseQuantity.Value,
                        QuotePrice = quote.Value,
                        UserId = userId
                    });
                    expenseId = expenseTransaction.Id;
                }
            }
            else if (dto.StockMovementType == "E")
            {
                var expenseAsset = await _assetRepository.GetByIdAsync(dto.ExpenseAssetId.Value)
                    ?? throw new NotFoundException("Expense asset not found");
                var expenseAccount = await _accountRepository.GetByIdAsync(dto.ExpenseAccountId.Value)
                    ?? throw new NotFoundException("Expense account not found");
                var expensePortfolio = await _portfolioRepository.GetByIdAsync(dto.ExpensePortfolioID.Value)
                    ?? throw new NotFoundException("Expense portfolio not found");

                var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = dto.ExpenseAccountId.Value,
                    Account = expenseAccount,
                    PortfolioId = expensePortfolio.Id,
                    Portfolio = expensePortfolio,
                    AssetId = dto.ExpenseAssetId.Value,
                    Asset = expenseAsset,
                    Date = dto.Date,
                    MovementType = "E",
                    TransactionClassId = null,
                    Detail = dto.CommerceType,
                    Amount = -(dto.ExpenseQuantity.Value),
                    QuotePrice = 1 / dto.ExpenseQuotePrice.Value,
                    UserId = userId
                });
                expenseId = expenseTransaction.Id;

                if (dto.CommerceType == "General")
                {
                    var incomeAsset = await _assetRepository.GetByIdAsync(dto.IncomeAssetId.Value)
                        ?? throw new NotFoundException("Income asset not found");
                    var incomeAccount = await _accountRepository.GetByIdAsync(dto.IncomeAccountId.Value)
                        ?? throw new NotFoundException("Income account not found");
                    var incomePortfolio = await _portfolioRepository.GetByIdAsync(dto.IncomePortfolioID.Value)
                        ?? throw new NotFoundException("Income portfolio not found");

                    var investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Ingreso Inversiones", userId)
                        ?? throw new BusinessRuleException("Transaction class 'Ingreso Inversiones' not found");

                    string? assetQuoteType = incomeAsset.Name == "Peso Argentino" ? "BLUE" : null;
                    var quote = await _assetQuoteRepository.GetLastQuoteByAsset(incomeAsset.Id, assetQuoteType);

                    var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                    {
                        AccountId = dto.IncomeAccountId.Value,
                        Account = incomeAccount,
                        PortfolioId = incomePortfolio.Id,
                        Portfolio = incomePortfolio,
                        AssetId = dto.IncomeAssetId.Value,
                        Asset = incomeAsset,
                        Date = dto.Date,
                        MovementType = "I",
                        TransactionClassId = investmentClass.Id,
                        Detail = dto.CommerceType,
                        Amount = dto.IncomeQuantity.Value,
                        QuotePrice = quote.Value,
                        UserId = userId
                    });
                    incomeId = incomeTransaction.Id;
                }
            }

            await _investmentTransactionRepository.AddAsync(new InvestmentTransaction
            {
                Date = dto.Date,
                Environment = dto.Environment,
                MovementType = dto.StockMovementType,
                CommerceType = dto.CommerceType,
                ExpenseTransactionId = expenseId == 0 ? null : (int?)expenseId,
                IncomeTransactionId = incomeId == 0 ? null : (int?)incomeId,
                UserId = userId
            });
        }

        public async Task<(IEnumerable<StockTransactionListDTO> Transactions, int TotalCount)> GetPaginatedStockTransactionsAsync(int userId, int page, int pageSize, string environment)
        {
            var (transactions, totalCount) = await _investmentTransactionRepository.GetPaginatedInvestmentTransactions(userId, page, pageSize, environment);

            var transactionsDTO = transactions.Select(m => new StockTransactionListDTO
            {
                Id = m.Id,
                Date = m.Date,
                StockMovementType = m.MovementType,
                CommerceType = m.CommerceType,
                AssetType = m.ExpenseTransaction == null || m.ExpenseTransaction.Asset.AssetType.Name == "Moneda"
                    ? m.IncomeTransaction.Asset.AssetType.Name
                    : m.ExpenseTransaction.Asset.AssetType.Name,
                ExpenseAsset = m.ExpenseTransaction?.Asset?.Name,
                ExpenseAccount = m.ExpenseTransaction?.Account?.Name,
                ExpensePortfolio = m.ExpenseTransaction?.Portfolio?.Name,
                ExpenseQuantity = m.ExpenseTransaction?.Amount,
                ExpenseQuotePrice = m.ExpenseTransaction?.Asset.Name == "Peso Argentino"
                    ? m.ExpenseTransaction?.QuotePrice
                    : 1 / m.ExpenseTransaction?.QuotePrice,
                IncomeAsset = m.IncomeTransaction?.Asset?.Name,
                IncomeAccount = m.IncomeTransaction?.Account?.Name,
                IncomePortfolio = m.IncomeTransaction?.Portfolio?.Name,
                IncomeQuantity = m.IncomeTransaction?.Amount,
                IncomeQuotePrice = m.IncomeTransaction?.Asset.Name == "Peso Argentino"
                    ? m.IncomeTransaction?.QuotePrice
                    : 1 / m.IncomeTransaction?.QuotePrice
            });

            return (transactionsDTO, totalCount);
        }

        public async Task DeleteStockTransactionAsync(int userId, int id)
        {
            var transaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id)
                ?? throw new NotFoundException("Stock transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (transaction.IncomeTransaction != null)
                    await _transactionRepository.DeleteAsync(transaction.IncomeTransaction.Id);
                if (transaction.ExpenseTransaction != null)
                    await _transactionRepository.DeleteAsync(transaction.ExpenseTransaction.Id);
                await _investmentTransactionRepository.DeleteAsync(transaction.Id);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        // ── Crypto ────────────────────────────────────────────────────────────

        public async Task CreateCryptoTransactionAsync(int userId, InvestmentTransactionAddDTO dto)
        {
            if (dto.Environment != "Crypto") throw new BusinessRuleException("Invalid environment");

            var incomeId = 0;
            var expenseId = 0;

            if (dto.MovementType == "I")
            {
                var incomeAsset = await _assetRepository.GetByIdAsync(dto.IncomeAssetId.Value)
                    ?? throw new NotFoundException("Income asset not found");
                var incomeAccount = await _accountRepository.GetByIdAsync(dto.IncomeAccountId.Value)
                    ?? throw new NotFoundException("Income account not found");
                var incomePortfolio = await _portfolioRepository.GetByIdAsync(dto.IncomePortfolioID.Value)
                    ?? throw new NotFoundException("Income portfolio not found");

                var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = dto.IncomeAccountId.Value,
                    Account = incomeAccount,
                    PortfolioId = incomePortfolio.Id,
                    Portfolio = incomePortfolio,
                    AssetId = dto.IncomeAssetId.Value,
                    Asset = incomeAsset,
                    Date = dto.Date,
                    MovementType = "I",
                    TransactionClassId = null,
                    Detail = dto.CommerceType,
                    Amount = dto.IncomeQuantity.Value,
                    QuotePrice = 1 / dto.IncomeQuotePrice.Value,
                    UserId = userId
                });
                incomeId = incomeTransaction.Id;

                if (dto.CommerceType == "Fiat/Crypto Commerce")
                {
                    var expenseAsset = await _assetRepository.GetByIdAsync(dto.ExpenseAssetId.Value)
                        ?? throw new NotFoundException("Expense asset not found");
                    var expenseAccount = await _accountRepository.GetByIdAsync(dto.ExpenseAccountId.Value)
                        ?? throw new NotFoundException("Expense account not found");
                    var expensePortfolio = await _portfolioRepository.GetByIdAsync(dto.ExpensePortfolioID.Value)
                        ?? throw new NotFoundException("Expense portfolio not found");

                    var balance = await _transactionRepository.GetBalance(expenseAccount.Id, expenseAsset.Id, expensePortfolio.Id);
                    if (balance < -dto.ExpenseQuantity.Value) throw new BusinessRuleException("Not enough balance");

                    var investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Inversiones", userId)
                        ?? throw new BusinessRuleException("Transaction class 'Inversiones' not found");

                    string? assetQuoteType = expenseAsset.Name == "Peso Argentino" ? "BLUE" : null;
                    var quote = await _assetQuoteRepository.GetLastQuoteByAsset(expenseAsset.Id, assetQuoteType);

                    var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                    {
                        AccountId = dto.ExpenseAccountId.Value,
                        Account = expenseAccount,
                        PortfolioId = expensePortfolio.Id,
                        Portfolio = expensePortfolio,
                        AssetId = dto.ExpenseAssetId.Value,
                        Asset = expenseAsset,
                        Date = dto.Date,
                        MovementType = "E",
                        TransactionClassId = investmentClass.Id,
                        Detail = dto.CommerceType,
                        Amount = -dto.ExpenseQuantity.Value,
                        QuotePrice = quote.Value,
                        UserId = userId
                    });
                    expenseId = expenseTransaction.Id;
                }
            }
            else if (dto.MovementType == "E")
            {
                var expenseAsset = await _assetRepository.GetByIdAsync(dto.ExpenseAssetId.Value)
                    ?? throw new NotFoundException("Expense asset not found");
                var expenseAccount = await _accountRepository.GetByIdAsync(dto.ExpenseAccountId.Value)
                    ?? throw new NotFoundException("Expense account not found");
                var expensePortfolio = await _portfolioRepository.GetByIdAsync(dto.ExpensePortfolioID.Value)
                    ?? throw new NotFoundException("Expense portfolio not found");

                var balance = await _transactionRepository.GetBalance(expenseAccount.Id, expenseAsset.Id, expensePortfolio.Id);
                if (balance < -dto.ExpenseQuantity.Value) throw new BusinessRuleException("Not enough balance");

                var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = dto.ExpenseAccountId.Value,
                    Account = expenseAccount,
                    PortfolioId = expensePortfolio.Id,
                    Portfolio = expensePortfolio,
                    AssetId = dto.ExpenseAssetId.Value,
                    Asset = expenseAsset,
                    Date = dto.Date,
                    MovementType = "E",
                    TransactionClassId = null,
                    Detail = dto.CommerceType,
                    Amount = -dto.ExpenseQuantity.Value,
                    QuotePrice = 1 / dto.ExpenseQuotePrice.Value,
                    UserId = userId
                });
                expenseId = expenseTransaction.Id;

                if (dto.CommerceType == "Fiat/Crypto Commerce")
                {
                    var incomeAsset = await _assetRepository.GetByIdAsync(dto.IncomeAssetId.Value)
                        ?? throw new NotFoundException("Income asset not found");
                    var incomeAccount = await _accountRepository.GetByIdAsync(dto.IncomeAccountId.Value)
                        ?? throw new NotFoundException("Income account not found");
                    var incomePortfolio = await _portfolioRepository.GetByIdAsync(dto.IncomePortfolioID.Value)
                        ?? throw new NotFoundException("Income portfolio not found");

                    var investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Ingreso Inversiones", userId)
                        ?? throw new BusinessRuleException("Transaction class 'Ingreso Inversiones' not found");

                    string? assetQuoteType = expenseAsset.Name == "Peso Argentino" ? "BLUE" : null;
                    var quote = await _assetQuoteRepository.GetLastQuoteByAsset(expenseAsset.Id, assetQuoteType);

                    var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                    {
                        AccountId = dto.IncomeAccountId.Value,
                        Account = incomeAccount,
                        PortfolioId = incomePortfolio.Id,
                        Portfolio = incomePortfolio,
                        AssetId = dto.IncomeAssetId.Value,
                        Asset = incomeAsset,
                        Date = dto.Date,
                        MovementType = "I",
                        TransactionClassId = null,
                        Detail = dto.CommerceType,
                        Amount = dto.IncomeQuantity.Value,
                        QuotePrice = quote.Value,
                        UserId = userId
                    });
                    incomeId = incomeTransaction.Id;
                }
            }
            else if (dto.MovementType == "EX")
            {
                var incomeAsset = await _assetRepository.GetByIdAsync(dto.IncomeAssetId.Value)
                    ?? throw new NotFoundException("Income asset not found");
                var incomeAccount = await _accountRepository.GetByIdAsync(dto.IncomeAccountId.Value)
                    ?? throw new NotFoundException("Income account not found");
                var incomePortfolio = await _portfolioRepository.GetByIdAsync(dto.IncomePortfolioID.Value)
                    ?? throw new NotFoundException("Income portfolio not found");

                var expenseAsset = await _assetRepository.GetByIdAsync(dto.ExpenseAssetId.Value)
                    ?? throw new NotFoundException("Expense asset not found");
                var expenseAccount = await _accountRepository.GetByIdAsync(dto.ExpenseAccountId.Value)
                    ?? throw new NotFoundException("Expense account not found");
                var expensePortfolio = await _portfolioRepository.GetByIdAsync(dto.ExpensePortfolioID.Value)
                    ?? throw new NotFoundException("Expense portfolio not found");

                var balance = await _transactionRepository.GetBalance(expenseAccount.Id, expenseAsset.Id, expensePortfolio.Id);
                if (balance < -dto.ExpenseQuantity.Value) throw new BusinessRuleException("Not enough balance");

                var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = dto.IncomeAccountId.Value,
                    Account = incomeAccount,
                    PortfolioId = incomePortfolio.Id,
                    Portfolio = incomePortfolio,
                    AssetId = dto.IncomeAssetId.Value,
                    Asset = incomeAsset,
                    Date = dto.Date,
                    MovementType = "I",
                    TransactionClassId = null,
                    Detail = dto.CommerceType,
                    Amount = dto.IncomeQuantity.Value,
                    QuotePrice = 1 / dto.IncomeQuotePrice.Value,
                    UserId = userId
                });
                incomeId = incomeTransaction.Id;

                var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = dto.ExpenseAccountId.Value,
                    Account = expenseAccount,
                    PortfolioId = expensePortfolio.Id,
                    Portfolio = expensePortfolio,
                    AssetId = dto.ExpenseAssetId.Value,
                    Asset = expenseAsset,
                    Date = dto.Date,
                    MovementType = "E",
                    TransactionClassId = null,
                    Detail = dto.CommerceType,
                    Amount = -dto.ExpenseQuantity.Value,
                    QuotePrice = 1 / dto.ExpenseQuotePrice.Value,
                    UserId = userId
                });
                expenseId = expenseTransaction.Id;
            }
            else
            {
                throw new BusinessRuleException("Invalid transaction type");
            }

            await _investmentTransactionRepository.AddAsync(new InvestmentTransaction
            {
                Date = dto.Date,
                Environment = dto.Environment,
                MovementType = dto.MovementType,
                CommerceType = dto.CommerceType,
                ExpenseTransactionId = expenseId == 0 ? null : (int?)expenseId,
                IncomeTransactionId = incomeId == 0 ? null : (int?)incomeId,
                UserId = userId
            });
        }

        public async Task<(IEnumerable<CryptoTransactionListDTO> Transactions, int TotalCount)> GetPaginatedCryptoTransactionsAsync(int userId, int page, int pageSize)
        {
            var (transactions, totalCount) = await _investmentTransactionRepository.GetPaginatedInvestmentTransactions(userId, page, pageSize, "Crypto");

            var transactionsDTO = transactions.Select(m => new CryptoTransactionListDTO
            {
                Id = m.Id,
                Date = m.Date,
                MovementType = m.MovementType,
                CommerceType = m.CommerceType,
                ExpenseAsset = m.ExpenseTransaction?.Asset?.Name,
                ExpenseAccount = m.ExpenseTransaction?.Account?.Name,
                ExpensePortfolio = m.ExpenseTransaction?.Portfolio?.Name,
                ExpenseAmount = m.ExpenseTransaction?.Amount,
                ExpenseQuote = m.ExpenseTransaction?.QuotePrice,
                IncomeAsset = m.IncomeTransaction?.Asset?.Name,
                IncomeAccount = m.IncomeTransaction?.Account?.Name,
                IncomePortfolio = m.IncomeTransaction?.Portfolio?.Name,
                IncomeAmount = m.IncomeTransaction?.Amount,
                IncomeQuote = m.IncomeTransaction?.QuotePrice
            }).ToList();

            foreach (var transaction in transactionsDTO)
            {
                if (transaction.ExpenseAsset != null && transaction.ExpenseAsset != "Peso Argentino")
                    transaction.ExpenseQuote = 1 / transaction.ExpenseQuote.Value;
                if (transaction.IncomeAsset != null && transaction.IncomeAsset != "Peso Argentino")
                    transaction.IncomeQuote = 1 / transaction.IncomeQuote.Value;
            }

            return (transactionsDTO, totalCount);
        }

        public async Task DeleteCryptoTransactionAsync(int userId, int id)
        {
            var transaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id)
                ?? throw new NotFoundException("Crypto transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (transaction.IncomeTransaction != null)
                    await _transactionRepository.DeleteAsync(transaction.IncomeTransaction.Id);
                if (transaction.ExpenseTransaction != null)
                    await _transactionRepository.DeleteAsync(transaction.ExpenseTransaction.Id);
                await _investmentTransactionRepository.DeleteAsync(transaction.Id);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        // ── Portfolio Transfers ───────────────────────────────────────────────

        public async Task CreatePortfolioTransactionAsync(int userId, PortfolioTransactionAddDTO dto)
        {
            var account = await _accountRepository.GetByIdAsync(dto.AccountId);
            if (account == null || account.UserId != userId) throw new BusinessRuleException("Invalid account.");

            var asset = await _assetRepository.GetByIdAsync(dto.AssetId)
                ?? throw new NotFoundException("Invalid asset.");

            var expensePortfolio = dto.ExpensePortfolioID.HasValue
                ? await _portfolioRepository.GetByIdAsync(dto.ExpensePortfolioID.Value)
                : null;
            if (dto.ExpensePortfolioID.HasValue && (expensePortfolio == null || expensePortfolio.UserId != userId))
                throw new BusinessRuleException("Invalid expense portfolio.");

            var incomePortfolio = dto.IncomePortfolioID.HasValue
                ? await _portfolioRepository.GetByIdAsync(dto.IncomePortfolioID.Value)
                : null;
            if (dto.IncomePortfolioID.HasValue && (incomePortfolio == null || incomePortfolio.UserId != userId))
                throw new BusinessRuleException("Invalid income portfolio.");

            if (dto.ExpensePortfolioID.HasValue)
            {
                var expenseBalance = await _transactionRepository.GetBalance(dto.AccountId, dto.AssetId, dto.ExpensePortfolioID.Value);
                if (expenseBalance < dto.Amount) throw new BusinessRuleException("Not enough balance in expense portfolio.");
            }

            var quotePrice = await _transactionRepository.GetAverageQuotePrice(dto.AccountId, dto.AssetId, dto.ExpensePortfolioID.Value);
            if (quotePrice == 0) throw new BusinessRuleException("Invalid quote price.");

            var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
            {
                AccountId = account.Id,
                Account = account,
                PortfolioId = expensePortfolio.Id,
                Portfolio = expensePortfolio,
                AssetId = asset.Id,
                Asset = asset,
                Date = dto.Date,
                MovementType = "E",
                TransactionClassId = null,
                Detail = "Portfolio Exchange",
                Amount = -dto.Amount,
                QuotePrice = quotePrice,
                UserId = userId
            });

            var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
            {
                AccountId = account.Id,
                Account = account,
                PortfolioId = incomePortfolio.Id,
                Portfolio = incomePortfolio,
                AssetId = asset.Id,
                Asset = asset,
                Date = dto.Date,
                MovementType = "I",
                TransactionClassId = null,
                Detail = "Portfolio Exchange",
                Amount = dto.Amount,
                QuotePrice = quotePrice,
                UserId = userId
            });

            await _investmentTransactionRepository.AddAsync(new InvestmentTransaction
            {
                Date = dto.Date,
                Environment = "PortfolioExchange",
                MovementType = "EX",
                CommerceType = "PortfolioExchange",
                ExpenseTransactionId = expenseTransaction.Id,
                IncomeTransactionId = incomeTransaction.Id,
                UserId = userId
            });
        }

        public async Task<(IEnumerable<CurrencyExchangeListDTO> Transactions, int TotalCount)> GetPaginatedPortfolioTransactionsAsync(int userId, int page, int pageSize)
        {
            var (transactions, totalCount) = await _investmentTransactionRepository.GetPaginatedInvestmentTransactions(userId, page, pageSize, "PortfolioExchange");

            var transactionsDTO = transactions.Select(m => new CurrencyExchangeListDTO
            {
                Id = m.Id,
                Date = m.Date,
                ExpenseAsset = m.ExpenseTransaction?.Asset?.Name,
                ExpenseAccount = m.ExpenseTransaction?.Account?.Name,
                ExpensePortfolio = m.ExpenseTransaction?.Portfolio?.Name,
                ExpenseAmount = m.ExpenseTransaction?.Amount,
                IncomeAsset = m.IncomeTransaction?.Asset?.Name,
                IncomeAccount = m.IncomeTransaction?.Account?.Name,
                IncomePortfolio = m.IncomeTransaction?.Portfolio?.Name,
                IncomeAmount = m.IncomeTransaction?.Amount
            });

            return (transactionsDTO, totalCount);
        }

        public async Task DeletePortfolioTransactionAsync(int userId, int id)
        {
            var transaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id)
                ?? throw new NotFoundException("Portfolio transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (transaction.IncomeTransaction != null)
                    await _transactionRepository.DeleteAsync(transaction.IncomeTransaction.Id);
                if (transaction.ExpenseTransaction != null)
                    await _transactionRepository.DeleteAsync(transaction.ExpenseTransaction.Id);
                await _investmentTransactionRepository.DeleteAsync(transaction.Id);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
