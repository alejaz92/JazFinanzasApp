using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.InvestmentTransaction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortFolioTransactionController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IInvestmentTransactionRepository _investmentTransactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IPortfolioRepository _portfolioRepository;

        public PortFolioTransactionController(ITransactionRepository transactionRepository,
            IInvestmentTransactionRepository investmentTransactionRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            IPortfolioRepository portfolioRepository)
        {
            _transactionRepository = transactionRepository;
            _investmentTransactionRepository = investmentTransactionRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _portfolioRepository = portfolioRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaginatedPortfolioTransaction(int page = 1, int pageSize = 10, string environment = "PortfolioExchange")
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            try
            {
                var (transactions, totalCount) = await _investmentTransactionRepository.GetPaginatedInvestmentTransactions(userId, page, pageSize, environment);

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

                return Ok(new { transactionsDTO, totalCount });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");

            }

        }

        [HttpPost]
        public async Task<IActionResult> CreatePortfolioTransaction([FromBody] PortfolioTransactionAddDTO transactionDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim.Value);
            if (transactionDTO == null)
            {
                return BadRequest("Invalid transaction data.");
            }

            // validate account, asset and portfolios
            var account = await _accountRepository.GetByIdAsync(transactionDTO.AccountId);
            if (account == null || account.UserId != userId)
            {
                return BadRequest("Invalid account.");
            }
            var asset = await _assetRepository.GetByIdAsync(transactionDTO.AssetId);
            if (asset == null)
            {
                return BadRequest("Invalid asset.");
            }
            var expensePortfolio = transactionDTO.ExpensePortfolioID.HasValue ?
                await _portfolioRepository.GetByIdAsync(transactionDTO.ExpensePortfolioID.Value) : null;
            if (transactionDTO.ExpensePortfolioID.HasValue && expensePortfolio == null && expensePortfolio.UserId != userId)
            {
                return BadRequest("Invalid expense portfolio.");
            }

            var incomePortfolio = transactionDTO.IncomePortfolioID.HasValue ?
                await _portfolioRepository.GetByIdAsync(transactionDTO.IncomePortfolioID.Value) : null;
            if (transactionDTO.IncomePortfolioID.HasValue && incomePortfolio == null && incomePortfolio.UserId != userId)
            {
                return BadRequest("Invalid income portfolio.");
            }

            // validate enaugh balance in expense portfolio
            if (transactionDTO.ExpensePortfolioID.HasValue)
            {
                var expenseBalance = await _transactionRepository.GetBalance(transactionDTO.AccountId, transactionDTO.AssetId, transactionDTO.ExpensePortfolioID.Value);
                if (expenseBalance < transactionDTO.Amount)
                {
                    return BadRequest("Not enough balance in expense portfolio.");
                }
            }

            // get quotePrice average
            var quotePrice = await _transactionRepository.GetAverageQuotePrice(transactionDTO.AccountId, transactionDTO.AssetId, transactionDTO.ExpensePortfolioID.Value);

            if (quotePrice == 0)
            {
                return BadRequest("Invalid quote price.");
            }

            var incomeId = 0;
            var expenseId = 0;

            try
            {
                //create transactions
                var expenseTransaction = new Transaction
                {
                    AccountId = account.Id,
                    Account = account,
                    PortfolioId = expensePortfolio.Id,
                    Portfolio = expensePortfolio,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = transactionDTO.Date,
                    MovementType = "E",
                    TransactionClassId = null,
                    Detail = "Portfolio Exchange",
                    Amount = -transactionDTO.Amount,
                    QuotePrice = quotePrice,
                    UserId = userId
                };

                var incomeTransaction = new Transaction
                {
                    AccountId = account.Id,
                    Account = account,
                    PortfolioId = incomePortfolio.Id,
                    Portfolio = incomePortfolio,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = transactionDTO.Date,
                    MovementType = "I",
                    TransactionClassId = null,
                    Detail = "Portfolio Exchange",
                    Amount = transactionDTO.Amount,
                    QuotePrice = quotePrice,
                    UserId = userId
                };

                //save transactions
                expenseTransaction = await _transactionRepository.AddAsyncReturnObject(expenseTransaction);
                expenseId = expenseTransaction.Id;

                incomeTransaction = await _transactionRepository.AddAsyncReturnObject(incomeTransaction);
                incomeId = incomeTransaction.Id;

                //create and save investment transaction
                var investmentTransaction = new InvestmentTransaction
                {
                    Date = transactionDTO.Date,
                    Environment = "PortfolioExchange",
                    MovementType = "EX",
                    CommerceType = "PortfolioExchange",
                    ExpenseTransactionId = expenseId,
                    IncomeTransactionId = incomeId,
                    UserId = userId
                };

                await _investmentTransactionRepository.AddAsync(investmentTransaction);
                return Ok();

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");

            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePortfolioTransaction(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var transaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id);

            if (transaction == null)
            {
                return NotFound();
            }

            if (transaction.UserId != userId)
            {
                return Unauthorized();
            }

            try
            {
                _investmentTransactionRepository.BeginTransactionAsync();

                if (transaction.IncomeTransaction != null)
                {
                    await _transactionRepository.DeleteAsync(transaction.IncomeTransaction.Id);
                }

                if (transaction.ExpenseTransaction != null)
                {
                    await _transactionRepository.DeleteAsync(transaction.ExpenseTransaction.Id);
                }

                await _investmentTransactionRepository.DeleteAsync(transaction.Id);

                await _investmentTransactionRepository.CommitTransactionAsync();

                return Ok();
            }
            catch
            {
                await _investmentTransactionRepository.RollbackTransactionAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }
        }
    }
}
