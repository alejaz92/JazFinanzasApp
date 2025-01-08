using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.InvestmentTransaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CryptoTransactionController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IInvestmentTransactionRepository _investmentTransactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;

        public CryptoTransactionController(
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
        public async Task<IActionResult> CreateCryptoTransaction(InvestmentTransactionAddDTO cryptoTransactionDTO)
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
                if (cryptoTransactionDTO.Environment == "Crypto")
                {
                    if (cryptoTransactionDTO.MovementType == "I")
                    {
                        // create income transaction

                        var incomeAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAssetId.Value);
                        var incomeAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAccountId.Value);
                        await CheckAssetsAndAccounts(incomeAsset,incomeAccount);   


                        var incomeTransaction = new Transaction
                        {
                            AccountId = cryptoTransactionDTO.IncomeAccountId.Value,
                            Account = incomeAccount,
                            AssetId = cryptoTransactionDTO.IncomeAssetId.Value,
                            Asset = incomeAsset,
                            Date = cryptoTransactionDTO.Date,
                            MovementType = "I",
                            TransactionClassId = null,
                            Detail = cryptoTransactionDTO.CommerceType,
                            Amount = cryptoTransactionDTO.IncomeQuantity.Value,
                            QuotePrice = 1/ cryptoTransactionDTO.IncomeQuotePrice.Value,
                            UserId = userId
                        };

                        incomeTransaction = await _transactionRepository.AddAsyncReturnObject(incomeTransaction);
                        incomeId = incomeTransaction.Id;



                        if (cryptoTransactionDTO.CommerceType == "Fiat/Crypto Commerce")
                        {
                            var expenseAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAssetId.Value);
                            var expenseAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAccountId.Value);
                            await CheckAssetsAndAccounts(expenseAsset,expenseAccount);

                            TransactionClass investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Inversiones", userId);
                            if (investmentClass == null)
                            {
                                return StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
                            }
                            string assetQuoteType = null;
                            if(expenseAsset.Name == "Peso Argentino")
                            {
                                assetQuoteType = "BLUE";
                            }

                            var quote = await _assetQuoteRepository.GetLastQuoteByAsset(expenseAsset.Id, assetQuoteType);

                            var expenseTransaction = new Transaction
                            {
                                AccountId = cryptoTransactionDTO.ExpenseAccountId.Value,
                                Account = expenseAccount,
                                AssetId = cryptoTransactionDTO.ExpenseAssetId.Value,
                                Asset = expenseAsset,
                                Date = cryptoTransactionDTO.Date,
                                MovementType = "E",
                                TransactionClassId = investmentClass.Id,
                                Detail = cryptoTransactionDTO.CommerceType,
                                Amount = -cryptoTransactionDTO.ExpenseQuantity.Value,
                                QuotePrice = quote.Value,
                                UserId = userId
                            };

                            expenseTransaction = await _transactionRepository.AddAsyncReturnObject(expenseTransaction);
                            expenseId = expenseTransaction.Id;
                        }
                    } else if (cryptoTransactionDTO.MovementType == "E")
                    {
                        // create expense transaction

                        var expenseAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAssetId.Value);
                        var expenseAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAccountId.Value);
                        await CheckAssetsAndAccounts(expenseAsset,expenseAccount);

                        var expenseTransaction = new Transaction
                        {
                            AccountId = cryptoTransactionDTO.ExpenseAccountId.Value,
                            Account = expenseAccount,
                            AssetId = cryptoTransactionDTO.ExpenseAssetId.Value,
                            Asset = expenseAsset,
                            Date = cryptoTransactionDTO.Date,
                            MovementType = "E",
                            TransactionClassId = null,
                            Detail = cryptoTransactionDTO.CommerceType,
                            Amount = -cryptoTransactionDTO.ExpenseQuantity.Value,
                            QuotePrice = 1/cryptoTransactionDTO.ExpenseQuotePrice.Value,
                            UserId = userId
                        };

                        expenseTransaction = await _transactionRepository.AddAsyncReturnObject(expenseTransaction);
                        expenseId = expenseTransaction.Id;

                        if (cryptoTransactionDTO.CommerceType == "Fiat/Crypto Commerce")
                        {
                            var incomeAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAssetId.Value);
                            var incomeAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAccountId.Value);
                            await CheckAssetsAndAccounts(incomeAsset,incomeAccount);

                            TransactionClass investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Ingreso Inversiones", userId);
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

                            var incomeTransaction = new Transaction
                            {
                                AccountId = cryptoTransactionDTO.IncomeAccountId.Value,
                                Account = incomeAccount,
                                AssetId = cryptoTransactionDTO.IncomeAssetId.Value,
                                Asset = incomeAsset,
                                Date = cryptoTransactionDTO.Date,
                                MovementType = "I",
                                TransactionClassId = null,
                                Detail = cryptoTransactionDTO.CommerceType,
                                Amount = cryptoTransactionDTO.IncomeQuantity.Value,
                                QuotePrice = quote.Value,
                                UserId = userId
                            };

                            incomeTransaction = await _transactionRepository.AddAsyncReturnObject(incomeTransaction);
                            incomeId = incomeTransaction.Id;
                        }

                    } else if (cryptoTransactionDTO.MovementType == "EX")
                    {
                        // create income transaction

                        var incomeAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAssetId.Value);
                        var incomeAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAccountId.Value);
                        await CheckAssetsAndAccounts(incomeAsset,incomeAccount);

                        var expenseAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAssetId.Value);
                        var expenseAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAccountId.Value);
                        await CheckAssetsAndAccounts(expenseAsset,expenseAccount);

                        var incomeTransaction = new Transaction
                        {
                            AccountId = cryptoTransactionDTO.IncomeAccountId.Value,
                            Account = incomeAccount,
                            AssetId = cryptoTransactionDTO.IncomeAssetId.Value,
                            Asset = incomeAsset,
                            Date = cryptoTransactionDTO.Date,
                            MovementType = "I",
                            TransactionClassId = null,
                            Detail = cryptoTransactionDTO.CommerceType,
                            Amount = cryptoTransactionDTO.IncomeQuantity.Value,
                            QuotePrice = 1/cryptoTransactionDTO.IncomeQuotePrice.Value,
                            UserId = userId
                        };

                        incomeTransaction = await _transactionRepository.AddAsyncReturnObject(incomeTransaction);
                        incomeId = incomeTransaction.Id;

                        var expenseTransaction = new Transaction
                        {
                            AccountId = cryptoTransactionDTO.ExpenseAccountId.Value,
                            Account = expenseAccount,
                            AssetId = cryptoTransactionDTO.ExpenseAssetId.Value,
                            Asset = expenseAsset,
                            Date = cryptoTransactionDTO.Date,
                            MovementType = "E",
                            TransactionClassId = null,
                            Detail = cryptoTransactionDTO.CommerceType,
                            Amount = -cryptoTransactionDTO.ExpenseQuantity.Value,
                            QuotePrice = 1/cryptoTransactionDTO.ExpenseQuotePrice.Value,
                            UserId = userId
                        };

                        expenseTransaction = await _transactionRepository.AddAsyncReturnObject(expenseTransaction);
                        expenseId = expenseTransaction.Id;

                    }
                    else
                    {
                        return BadRequest("Invalid transaction type");
                    }



                    var investmentTransaction = new InvestmentTransaction
                    {
                        Date = cryptoTransactionDTO.Date,
                        Environment = cryptoTransactionDTO.Environment,
                        MovementType = cryptoTransactionDTO.MovementType,
                        CommerceType = cryptoTransactionDTO.CommerceType,
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
                }
                else
                {
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
        public async Task<IActionResult> GetPaginatedCryptoTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var (transactions, totalCount) = await _investmentTransactionRepository.GetPaginatedInvestmentTransactions(userId, page, pageSize, "Crypto");

            var transactionsDTO = transactions.Select(m => new CryptoTransactionListDTO
            {
                Id = m.Id,
                Date = m.Date,
                MovementType = m.MovementType,
                CommerceType = m.CommerceType,
                ExpenseAsset = m.ExpenseTransaction?.Asset?.Name,
                ExpenseAccount = m.ExpenseTransaction?.Account?.Name,
                ExpenseAmount = m.ExpenseTransaction?.Amount,
                ExpenseQuote = m.ExpenseTransaction?.QuotePrice,
                IncomeAsset = m.IncomeTransaction?.Asset?.Name,
                IncomeAccount = m.IncomeTransaction?.Account?.Name,
                IncomeAmount = m.IncomeTransaction?.Amount,
                IncomeQuote = m.IncomeTransaction?.QuotePrice
            }).ToList();


            //if (Asset != "Peso Argentino") then quote = 1/quote
            foreach (var transaction in transactionsDTO)
            {
                if (transaction.ExpenseAsset != null && transaction.ExpenseAsset != "Peso Argentino")
                {
                    transaction.ExpenseQuote = 1 / transaction.ExpenseQuote.Value;
                }
                if (transaction.IncomeAsset != null && transaction.IncomeAsset != "Peso Argentino")
                {
                    transaction.IncomeQuote = 1 / transaction.IncomeQuote.Value;
                }
            }

            return Ok(new { transactions = transactionsDTO, totalCount });


        }

        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetCryptoTransactionById(int id)
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        //    if (userIdClaim == null)
        //    {
        //        return Unauthorized();
        //    }

        //    int userId = int.Parse(userIdClaim.Value);

        //    var transaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id);

        //    if (transaction == null)
        //    {
        //        return NotFound();
        //    }

        //    if (transaction.UserId != userId)
        //    {
        //        return Unauthorized();
        //    }

        //    var transactionDTO = new CryptoTransactionListDTO
        //    {
        //        Id = transaction.Id,
        //        Date = transaction.Date,
        //        MovementType = transaction.MovementType,
        //        CommerceType = transaction.CommerceType,
        //        ExpenseAsset = transaction.ExpenseTransaction?.Asset?.Name,
        //        ExpenseAccount = transaction.ExpenseTransaction?.Account?.Name,
        //        ExpenseAmount = transaction.ExpenseTransaction?.Amount,
        //        ExpenseQuote = transaction.ExpenseTransaction?.QuotePrice,
        //        IncomeAsset = transaction.IncomeTransaction?.Asset?.Name,
        //        IncomeAccount = transaction.IncomeTransaction?.Account?.Name,
        //        IncomeAmount = transaction.IncomeTransaction?.Amount,
        //        IncomeQuote = transaction.IncomeTransaction?.QuotePrice
        //    };

        //    return Ok(transactionDTO);
        //}

        // delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCryptoTransaction(int id)
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
