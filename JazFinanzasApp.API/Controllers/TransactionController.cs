using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;
using JazFinanzasApp.API.Business.DTO.Transaction;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IInvestmentTransactionRepository _investmentTransactionRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        public TransactionController(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            ITransactionClassRepository transactionClassRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IInvestmentTransactionRepository investmentTransactionRepository,
            IPortfolioRepository portfolioRepository
            )
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _transactionClassRepository = transactionClassRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _investmentTransactionRepository = investmentTransactionRepository;
            _portfolioRepository = portfolioRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaginatedTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);


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
                MovementType = m.MovementType

            }).ToList();

            return Ok(new { Transactions = transactionsDTO, TotalCount = totalCount });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction(TransactionAddDTO transactionDTO)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var defaultPortfolio = await _portfolioRepository.GetDefaultPortfolio(userId);
            if (defaultPortfolio == null)
            {
                return NotFound("Default portfolio not found");
            }


            var asset = await _assetRepository.GetByIdAsync(transactionDTO.assetId);
            if (asset == null)
            {
                return NotFound();
            }
            decimal quotePrice = 0;


            if (asset.Symbol == "USD")
            {
                quotePrice = 1;
            }
            else if (transactionDTO.quotePrice != 0)
            {
                if (asset.Symbol == "ARS")
                {
                    quotePrice = transactionDTO.quotePrice;
                }
                else
                {
                    quotePrice = 1 / transactionDTO.quotePrice;
                }
            }
            else
            {
                string type;
                if (asset.Symbol == "ARS")
                {
                    type = "BLUE";
                }
                else
                {
                    type = "NA";
                }

                quotePrice = await _assetQuoteRepository.GetQuotePrice(asset.Id, transactionDTO.date, type);
            }





            if (transactionDTO.movementType == "I")
            {
                var incomeAccount = await _accountRepository.GetByIdAsync(transactionDTO.incomeAccountId.Value);
                if (incomeAccount == null)
                {
                    return NotFound();
                }
                if (incomeAccount.UserId != userId)
                {
                    return Unauthorized();
                }
                var transactionClass = await _transactionClassRepository.GetByIdAsync(transactionDTO.transactionClassId.Value);
                if (transactionClass == null)
                {
                    return NotFound();
                }
                if (transactionClass.IncExp == "E")
                {
                    return BadRequest("No se puede asignar una clase de movimiento de tipo egreso a un movimiento de tipo ingreso");
                }
                if (transactionClass.UserId != userId)
                {
                    return Unauthorized();
                }



                var transaction = new Transaction
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
                };

                await _transactionRepository.AddAsync(transaction);

            }
            else if (transactionDTO.movementType == "E")
            {
                var expenseAccount = await _accountRepository.GetByIdAsync(transactionDTO.expenseAccountId.Value);
                if (expenseAccount == null)
                {
                    return NotFound();
                }
                if (expenseAccount.UserId != userId)
                {
                    return Unauthorized();
                }

                var transactionClass = await _transactionClassRepository.GetByIdAsync(transactionDTO.transactionClassId.Value);
                if (transactionClass == null)
                {
                    return NotFound();
                }
                if (transactionClass.IncExp == "I")
                {
                    return BadRequest("No se puede asignar una clase de movimiento de tipo ingreso a un movimiento de tipo egreso");
                }
                if (transactionClass.UserId != userId)
                {
                    return Unauthorized();
                }

                // check if there is eneugh balance in the account
                var balance = await _transactionRepository.GetBalance(transactionDTO.expenseAccountId.Value, asset.Id, defaultPortfolio.Id);

                if (balance < transactionDTO.amount)
                {
                    return BadRequest("No hay suficiente saldo en la cuenta");
                }



                var transaction = new Transaction
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
                    QuotePrice = quotePrice
                };
                await _transactionRepository.AddAsync(transaction);
            }
            else if (transactionDTO.movementType == "EX")
            {
                var time = DateTime.UtcNow;

                var incomeAccount = await _accountRepository.GetByIdAsync(transactionDTO.incomeAccountId.Value);
                if (incomeAccount == null)
                {
                    return NotFound();
                }
                if (incomeAccount.UserId != userId)
                {
                    return Unauthorized();
                }

                var expenseAccount = await _accountRepository.GetByIdAsync(transactionDTO.expenseAccountId.Value);
                if (expenseAccount == null)
                {
                    return NotFound();
                }
                if (expenseAccount.UserId != userId)
                {
                    return Unauthorized();
                }

                // check if there is eneugh balance in the account
                var balance = await _transactionRepository.GetBalance(transactionDTO.expenseAccountId.Value, asset.Id, defaultPortfolio.Id);
                if (balance < transactionDTO.amount)
                {
                    return BadRequest("No hay suficiente saldo en la cuenta");
                }


                var incomeTransaction = new Transaction
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
                };

                incomeTransaction = await _transactionRepository.AddAsyncReturnObject(incomeTransaction);



                var expenseTransaction = new Transaction
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
                };
                expenseTransaction = await _transactionRepository.AddAsyncReturnObject(expenseTransaction);

                var investmentTransaction = new InvestmentTransaction
                {
                    Date = transactionDTO.date,
                    Environment = "Exchange",
                    MovementType = "EX",
                    CommerceType = "Exchange",
                    IncomeTransactionId = incomeTransaction.Id,
                    ExpenseTransactionId = expenseTransaction.Id,
                    UserId = userId
                };

                await _investmentTransactionRepository.AddAsync(investmentTransaction);
            }



            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditTransaction(int id, TransactionEditDTO transactionDTO)
        {



            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }
            if (transaction.UserId != userId)
            {
                return Unauthorized();
            }

            transaction.Amount = transactionDTO.Amount;

            if (transaction.TransactionClassId != transactionDTO.TransactionClassId)
            {
                var transactionClass = await _transactionClassRepository.GetByIdAsync(transactionDTO.TransactionClassId);
                if (transactionClass == null)
                {
                    return NotFound();
                }
                if (transactionClass.UserId != userId)
                {
                    return Unauthorized();
                }
                transaction.TransactionClassId = transactionDTO.TransactionClassId;
            }

            if (transaction.AccountId != transactionDTO.AccountID)
            {
                var account = await _accountRepository.GetByIdAsync(transactionDTO.AccountID);
                if (account == null)
                {
                    return NotFound();
                }
                if (account.UserId != userId)
                {
                    return Unauthorized();
                }
                transaction.AccountId = transactionDTO.AccountID;
            }

            // if the date and/or the asset are changed, the quote price must be recalculated
            if (transaction.Date != transactionDTO.Date || transaction.AssetId != transactionDTO.AssetId)
            {
                var asset = await _assetRepository.GetByIdAsync(transactionDTO.AssetId);


                string type;

                if (asset.Symbol == "ARS")
                {
                    type = "BLUE";
                }
                else
                {
                    type = "NA";
                }
                decimal quotePrice = 0;
                if (asset.Symbol == "USD")
                {
                    quotePrice = 1;
                }
                else
                {
                    quotePrice = await _assetQuoteRepository.GetQuotePrice(asset.Id, transactionDTO.Date, type);
                }


                transaction.QuotePrice = quotePrice;
                transaction.Date = transactionDTO.Date;
                transaction.AssetId = transactionDTO.AssetId;
            }

            if (transaction.MovementType == "E" && transaction.Amount > 0)
            {
                transaction.Amount = -transactionDTO.Amount;
            }
            else
            {
                transaction.Amount = transactionDTO.Amount;
            }

            transaction.Detail = transactionDTO.Detail;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(transaction);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var transaction = await _transactionRepository.GetByIdAsync(id);

            if (transaction == null)
            {
                return NotFound();
            }
            if (transaction.UserId != userId)
            {
                return Unauthorized();
            }

            await _transactionRepository.DeleteAsync(transaction.Id);

            return Ok();
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetTransaction(int Id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var transaction = await _transactionRepository.GetTransactionByIdAsync(Id);
            if (transaction == null)
            {
                return NotFound();
            }
            if (transaction.UserId != userId)
            {
                return Unauthorized();
            }

            var transactionDTO = new TransactionListDTO
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
                TransactionClassId = transaction.TransactionClassId,
                TransactionClassName = transaction.TransactionClass.Description,
                MovementType = transaction.MovementType
            };

            return Ok(transactionDTO);
        }

        [HttpPost("refund/{Id}")]
        public async Task<IActionResult> RefundTransaction(int Id, [FromBody] RefundDTO refundDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var transaction = await _transactionRepository.GetByIdAsync(Id);
            if (transaction == null)
            {
                return NotFound();
            }
            if (transaction.UserId != userId)
            {
                return Unauthorized();
            }

            if (transaction.MovementType == "I")
            {
                return BadRequest("Cannot refund an income transaction");
            }

            var refundAccount = await _accountRepository.GetByIdAsync(refundDTO.AccountId);
            if (refundAccount == null)
            {
                return NotFound();
            }



            transaction.Amount = transaction.Amount + refundDTO.Amount;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction);

            if (transaction.AccountId != refundAccount.Id)
            {
                var time = DateTime.UtcNow;

                var refundExpenseTransaction = new Transaction
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
                };
                await _transactionRepository.AddAsync(refundExpenseTransaction);

                var refundIncomeTransaction = new Transaction
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
                };
                await _transactionRepository.AddAsync(refundIncomeTransaction);
            }
            return Ok();
        }

        // get paginated exchange transactions
        [HttpGet("exchange")]
        public async Task<IActionResult> GetPaginatedExchangeTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

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

            return Ok(new { Transactions = transactionsDTO, TotalCount = totalCount });
        }

        [HttpDelete("exchange/{id}")]
        public async Task<IActionResult> DeleteExchangeTransaction(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            try
            {
                var investmentTransaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id);

                if (investmentTransaction == null)
                {
                    return NotFound();
                }

                if (investmentTransaction.UserId != userId)
                {
                    return Unauthorized();
                }


                try
                {
                    await _investmentTransactionRepository.BeginTransactionAsync();

                    if (investmentTransaction.IncomeTransaction != null)
                    {
                        await _transactionRepository.DeleteAsync(investmentTransaction.IncomeTransactionId.Value);
                    }

                    if (investmentTransaction.ExpenseTransaction != null)
                    {
                        await _transactionRepository.DeleteAsync(investmentTransaction.ExpenseTransactionId.Value);
                    }

                    await _investmentTransactionRepository.DeleteAsync(investmentTransaction.Id);
                    await _investmentTransactionRepository.CommitTransactionAsync();
                    return Ok();
                }
                catch
                {
                    await _investmentTransactionRepository.RollbackTransactionAsync();
                    return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
                }

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }
        }

        // get balance by asset, account and portfolio

    }       
}
