using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO;
using JazFinanzasApp.API.Models.DTO.InvestmentTransaction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockTransactionController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IInvestmentTransactionRepository _investmentTransactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;

        public StockTransactionController(
            ITransactionRepository transactionRepository,
            IInvestmentTransactionRepository investmentTransactionRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            IAssetQuoteRepository assetQuoteRepository,
            ITransactionClassRepository transactionClassRepository
            )
        {
            _transactionRepository = transactionRepository;
            _investmentTransactionRepository = investmentTransactionRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _transactionClassRepository = transactionClassRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateStockTransaction(StockTransactionAddDTO stockTransactionDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);
            var incomeId = 0;
            var expenseId = 0;

            try
            {
                if (stockTransactionDto.Environment == "Stock")
                {
                    if (stockTransactionDto.StockMovementType == "I")
                    {
                        var incomeAsset = await _assetRepository.GetByIdAsync(stockTransactionDto.IncomeAssetId.Value);
                        var incomeAccount = await _accountRepository.GetByIdAsync(stockTransactionDto.IncomeAccountId.Value);
                        await CheckAssetsAndAccounts(incomeAsset, incomeAccount);

                        var incomeTransaction = new Transaction
                        {
                            AccountId = stockTransactionDto.IncomeAccountId.Value,
                            Account = incomeAccount,
                            AssetId = stockTransactionDto.IncomeAssetId.Value,
                            Asset = incomeAsset,
                            Date = stockTransactionDto.Date,
                            MovementType = "I",
                            TransactionClassId = null,
                            Detail = stockTransactionDto.CommerceType,
                            Amount = stockTransactionDto.IncomeQuantity.Value,
                            QuotePrice = 1 / stockTransactionDto.IncomeQuotePrice.Value,
                            UserId = userId
                        };

                        incomeTransaction = await _transactionRepository.AddAsyncReturnObject(incomeTransaction);
                        incomeId = incomeTransaction.Id;

                        if (stockTransactionDto.CommerceType == "General")
                        {
                            var expenseAsset = await _assetRepository.GetByIdAsync(stockTransactionDto.ExpenseAssetId.Value);
                            var expenseAccount = await _accountRepository.GetByIdAsync(stockTransactionDto.ExpenseAccountId.Value);
                            await CheckAssetsAndAccounts(expenseAsset, expenseAccount);

                            TransactionClass investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Inversiones");
                            if (investmentClass == null)
                            {
                                return StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
                            }
                            string assetQuoteType = null;
                            if (expenseAsset.Name == "Peso Argentino")
                            {
                                assetQuoteType = "BLUE";
                            }

                            var quote = await _assetQuoteRepository.GetLastQuoteByAsset(expenseAsset.Id, assetQuoteType);

                            var expenseTransaction = new Transaction
                            {
                                AccountId = stockTransactionDto.ExpenseAccountId.Value,
                                Account = expenseAccount,
                                AssetId = stockTransactionDto.ExpenseAssetId.Value,
                                Asset = expenseAsset,
                                Date = stockTransactionDto.Date,
                                MovementType = "E",
                                TransactionClassId = investmentClass.Id,
                                Detail = stockTransactionDto.CommerceType,
                                Amount = -stockTransactionDto.ExpenseQuantity.Value,
                                QuotePrice = quote.Value,
                                UserId = userId
                            };

                            expenseTransaction = await _transactionRepository.AddAsyncReturnObject(expenseTransaction);
                            expenseId = expenseTransaction.Id;
                        }
                    }
                    else if (stockTransactionDto.StockMovementType == "E")
                    {
                        var expenseAsset = await _assetRepository.GetByIdAsync(stockTransactionDto.ExpenseAssetId.Value);
                        var expenseAccount = await _accountRepository.GetByIdAsync(stockTransactionDto.ExpenseAccountId.Value);
                        await CheckAssetsAndAccounts(expenseAsset, expenseAccount);

                        var expenseTransaction = new Transaction
                        {
                            AccountId = stockTransactionDto.ExpenseAccountId.Value,
                            Account = expenseAccount,
                            AssetId = stockTransactionDto.ExpenseAssetId.Value,
                            Asset = expenseAsset,
                            Date = stockTransactionDto.Date,
                            MovementType = "E",
                            TransactionClassId = null,
                            Detail = stockTransactionDto.CommerceType,
                            Amount = -stockTransactionDto.ExpenseQuantity.Value,
                            QuotePrice = stockTransactionDto.ExpenseQuotePrice.Value,
                            UserId = userId
                        };

                        expenseTransaction = await _transactionRepository.AddAsyncReturnObject(expenseTransaction);
                        expenseId = expenseTransaction.Id;

                        if (stockTransactionDto.CommerceType == "General")
                        {
                            var incomeAsset = await _assetRepository.GetByIdAsync(stockTransactionDto.IncomeAssetId.Value);
                            var incomeAccount = await _accountRepository.GetByIdAsync(stockTransactionDto.IncomeAccountId.Value);
                            await CheckAssetsAndAccounts(incomeAsset, incomeAccount);

                            TransactionClass investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Inversiones");
                            if (investmentClass == null)
                            {
                                return StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
                            }
                            string assetQuoteType = null;
                            if (incomeAsset.Name == "Peso Argentino")
                            {
                                assetQuoteType = "BLUE";
                            }

                            var quote = await _assetQuoteRepository.GetLastQuoteByAsset(incomeAsset.Id, assetQuoteType);

                            var incomeTransaction = new Transaction
                            {
                                AccountId = stockTransactionDto.IncomeAccountId.Value,
                                Account = incomeAccount,
                                AssetId = stockTransactionDto.IncomeAssetId.Value,
                                Asset = incomeAsset,
                                Date = stockTransactionDto.Date,
                                MovementType = "I",
                                TransactionClassId = investmentClass.Id,
                                Detail = stockTransactionDto.CommerceType,
                                Amount = stockTransactionDto.IncomeQuantity.Value,
                                QuotePrice = quote.Value,
                                UserId = userId
                            };

                            incomeTransaction = await _transactionRepository.AddAsyncReturnObject(incomeTransaction);
                            incomeId = incomeTransaction.Id;
                        }
                    }

                    var investmentTransaction = new InvestmentTransaction
                    {
                        Date = stockTransactionDto.Date,
                        Environment = stockTransactionDto.Environment,
                        MovementType = stockTransactionDto.StockMovementType,
                        CommerceType = stockTransactionDto.CommerceType,
                        ExpenseTransactionId = expenseId,
                        IncomeTransactionId = incomeId,
                        UserId = userId
                    };

                    if (investmentTransaction.ExpenseTransactionId == 0)
                    {
                        investmentTransaction.ExpenseTransactionId = null;
                    }
                    if (investmentTransaction.IncomeTransactionId == 0)
                    {
                        investmentTransaction.IncomeTransactionId = null;
                    }

                    await _investmentTransactionRepository.AddAsync(investmentTransaction);

                    return Ok();
                } else                 {
                    return BadRequest("Invalid environment");
                }

            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }
        }

        private async Task<IActionResult> CheckAssetsAndAccounts(Asset? asset, Account? account)
        {
            if (asset == null)
            {
                return BadRequest("Invalid asset");
            }
            if (account == null)
            {
                return BadRequest("Invalid account");
            }
            return null;
        }

        [HttpGet] 
        public async Task<IActionResult> GetPaginatedStockTransactions(int page = 1, int pageSize = 10, string environment = "Stock")
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

                var transactionsDTO = transactions.Select(m => new StockTransactionListDTO
                {
                    Id = m.Id,
                    Date = m.Date,
                    StockMovementType = m.MovementType,
                    CommerceType = m.CommerceType,
                    AssetType =
                        m.ExpenseTransaction == null || m.ExpenseTransaction.Asset.AssetType.Name == "Moneda"
                        ? m.IncomeTransaction.Asset.AssetType.Name
                        : m.ExpenseTransaction.Asset.AssetType.Name,
                    ExpenseAsset = m.ExpenseTransaction?.Asset?.Name,
                    ExpenseAccount = m.ExpenseTransaction?.Account?.Name,
                    ExpenseQuantity = m.ExpenseTransaction?.Amount,
                    ExpenseQuotePrice = m.ExpenseTransaction?.Asset.Name == "Peso Argentino"
                        ? m.ExpenseTransaction?.QuotePrice
                        : 1 / m.ExpenseTransaction?.QuotePrice,
                    IncomeAsset = m.IncomeTransaction?.Asset?.Name,
                    IncomeAccount = m.IncomeTransaction?.Account?.Name,
                    IncomeQuantity = m.IncomeTransaction?.Amount,
                    IncomeQuotePrice = m.IncomeTransaction?.Asset.Name == "Peso Argentino"
                        ? m.IncomeTransaction?.QuotePrice
                        : 1/m.IncomeTransaction?.QuotePrice
                });

                

                return Ok(new { transactionsDTO, totalCount });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockTransaction(int id)
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
                await _investmentTransactionRepository.BeginTransactionAsync();

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
