using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class FiatCurrencyExchangeService : IFiatCurrencyExchangeService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IInvestmentTransactionRepository _investmentTransactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IPortfolioRepository _portfolioRepository;

        public FiatCurrencyExchangeService(
            ITransactionRepository transactionRepository,
            IInvestmentTransactionRepository investmentTransactionRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IPortfolioRepository portfolioRepository)
        {
            _transactionRepository = transactionRepository;
            _investmentTransactionRepository = investmentTransactionRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _portfolioRepository = portfolioRepository;
        }

        public async Task<(IEnumerable<CurrencyExchangeListDTO> Transactions, int TotalCount)> GetPaginatedAsync(int userId, int page, int pageSize)
        {
            var (transactions, totalCount) = await _investmentTransactionRepository.GetPaginatedInvestmentTransactions(userId, page, pageSize, "CurrencyExchange");

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

        public async Task CreateExchangeTransactionAsync(int userId, CurrencyExchangeAddDTO dto)
        {
            var defaultPortfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                ?? throw new NotFoundException("Default portfolio not found");

            var expenseAsset = await _assetRepository.GetByIdAsync(dto.ExpenseAssetId.Value)
                ?? throw new NotFoundException("Expense asset not found");
            var expenseAccount = await _accountRepository.GetByIdAsync(dto.ExpenseAccountId.Value)
                ?? throw new NotFoundException("Expense account not found");
            var incomeAsset = await _assetRepository.GetByIdAsync(dto.IncomeAssetId.Value)
                ?? throw new NotFoundException("Income asset not found");
            var incomeAccount = await _accountRepository.GetByIdAsync(dto.IncomeAccountId.Value)
                ?? throw new NotFoundException("Income account not found");

            var expenseBalance = await _transactionRepository.GetBalance(expenseAccount.Id, expenseAsset.Id, defaultPortfolio.Id);
            if (expenseBalance < dto.ExpenseAmount)
                throw new BusinessRuleException("Not enough balance in the expense account");

            string? expenseQuoteType = expenseAsset.Name == "Peso Argentino" ? "BLUE" : null;
            var expenseQuote = await _assetQuoteRepository.GetLastQuoteByAsset(expenseAsset.Id, expenseQuoteType);

            string? incomeQuoteType = incomeAsset.Name == "Peso Argentino" ? "BLUE" : null;
            var incomeQuote = await _assetQuoteRepository.GetLastQuoteByAsset(incomeAsset.Id, incomeQuoteType);

            var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
            {
                AccountId = dto.ExpenseAccountId.Value,
                Account = expenseAccount,
                PortfolioId = defaultPortfolio.Id,
                Portfolio = defaultPortfolio,
                AssetId = dto.ExpenseAssetId.Value,
                Asset = expenseAsset,
                Date = dto.Date,
                MovementType = "E",
                TransactionClassId = null,
                Detail = "Currency Exchange",
                Amount = -dto.ExpenseAmount.Value,
                QuotePrice = expenseQuote.Value,
                UserId = userId
            });

            var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
            {
                AccountId = dto.IncomeAccountId.Value,
                Account = incomeAccount,
                PortfolioId = defaultPortfolio.Id,
                Portfolio = defaultPortfolio,
                AssetId = dto.IncomeAssetId.Value,
                Asset = incomeAsset,
                Date = dto.Date,
                MovementType = "I",
                TransactionClassId = null,
                Detail = "Currency Exchange",
                Amount = dto.IncomeAmount.Value,
                QuotePrice = incomeQuote.Value,
                UserId = userId
            });

            await _investmentTransactionRepository.AddAsync(new InvestmentTransaction
            {
                Date = dto.Date,
                Environment = "CurrencyExchange",
                MovementType = "EX",
                CommerceType = "CurrencyExchange",
                ExpenseTransactionId = expenseTransaction.Id,
                IncomeTransactionId = incomeTransaction.Id,
                UserId = userId
            });
        }

        public async Task DeleteExchangeTransactionAsync(int userId, int id)
        {
            var investmentTransaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id)
                ?? throw new NotFoundException("Exchange transaction not found");
            if (investmentTransaction.UserId != userId) throw new UnauthorizedDomainException();

            if (investmentTransaction.IncomeTransaction != null)
                await _transactionRepository.DeleteAsync(investmentTransaction.IncomeTransactionId.Value);
            if (investmentTransaction.ExpenseTransaction != null)
                await _transactionRepository.DeleteAsync(investmentTransaction.ExpenseTransactionId.Value);
            await _investmentTransactionRepository.DeleteAsync(investmentTransaction.Id);
        }
    }
}
