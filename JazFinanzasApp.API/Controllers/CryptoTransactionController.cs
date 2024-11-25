using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.InvestmentMovement;
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
        private readonly IMovementRepository _movementRepository;
        private readonly IInvestmentMovementRepository _investmentMovementRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;

        public CryptoTransactionController(
            IMovementRepository movementRepository,
            IInvestmentMovementRepository investmentMovementRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            IAssetQuoteRepository assetQuoteRepository,
            ITransactionClassRepository transactionClassRepository
            )
        {
            _movementRepository = movementRepository;
            _investmentMovementRepository = investmentMovementRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _transactionClassRepository = transactionClassRepository;
        }


        [HttpPost]
        public async Task<IActionResult> CreateCryptoTransaction(InvestmentMovementAddDTO cryptoTransactionDTO)
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
                        // create income movement

                        var incomeAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAssetId.Value);
                        var incomeAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAccountId.Value);
                        await CheckAssetsAndAccounts(incomeAsset,incomeAccount);   


                        var incomeMovement = new Movement
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

                        incomeMovement = await _movementRepository.AddAsyncReturnObject(incomeMovement);
                        incomeId = incomeMovement.Id;



                        if (cryptoTransactionDTO.CommerceType == "Fiat/Crypto Commerce")
                        {
                            var expenseAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAssetId.Value);
                            var expenseAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAccountId.Value);
                            await CheckAssetsAndAccounts(expenseAsset,expenseAccount);

                            TransactionClass investmentClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Inversiones");
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

                            var expenseMovement = new Movement
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

                            expenseMovement = await _movementRepository.AddAsyncReturnObject(expenseMovement);
                            expenseId = expenseMovement.Id;
                        }
                    } else if (cryptoTransactionDTO.MovementType == "E")
                    {
                        // create expense movement

                        var expenseAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAssetId.Value);
                        var expenseAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAccountId.Value);
                        await CheckAssetsAndAccounts(expenseAsset,expenseAccount);

                        var expenseMovement = new Movement
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

                        expenseMovement = await _movementRepository.AddAsyncReturnObject(expenseMovement);
                        expenseId = expenseMovement.Id;

                        if (cryptoTransactionDTO.CommerceType == "Fiat/Crypto Commerce")
                        {
                            var incomeAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAssetId.Value);
                            var incomeAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAccountId.Value);
                            await CheckAssetsAndAccounts(incomeAsset,incomeAccount);

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

                            var incomeMovement = new Movement
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

                            incomeMovement = await _movementRepository.AddAsyncReturnObject(incomeMovement);
                            incomeId = incomeMovement.Id;
                        }

                    } else if (cryptoTransactionDTO.MovementType == "EX")
                    {
                        // create income movement

                        var incomeAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAssetId.Value);
                        var incomeAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.IncomeAccountId.Value);
                        await CheckAssetsAndAccounts(incomeAsset,incomeAccount);

                        var expenseAsset = await _assetRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAssetId.Value);
                        var expenseAccount = await _accountRepository.GetByIdAsync(cryptoTransactionDTO.ExpenseAccountId.Value);
                        await CheckAssetsAndAccounts(expenseAsset,expenseAccount);

                        var incomeMovement = new Movement
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

                        incomeMovement = await _movementRepository.AddAsyncReturnObject(incomeMovement);
                        incomeId = incomeMovement.Id;

                        var expenseMovement = new Movement
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

                        expenseMovement = await _movementRepository.AddAsyncReturnObject(expenseMovement);
                        expenseId = expenseMovement.Id;

                    }
                    else
                    {
                        return BadRequest("Invalid movement type");
                    }



                    var investmentMovement = new InvestmentMovement
                    {
                        Date = cryptoTransactionDTO.Date,
                        Environment = cryptoTransactionDTO.Environment,
                        MovementType = cryptoTransactionDTO.MovementType,
                        CommerceType = cryptoTransactionDTO.CommerceType,
                        ExpenseMovementId = expenseId,
                        IncomeMovementId = incomeId,
                        UserId = userId
                    };

                    if (investmentMovement.ExpenseMovementId == 0)
                    {
                        investmentMovement.ExpenseMovementId = null;
                    }
                    if (investmentMovement.IncomeMovementId == 0)
                    {
                        investmentMovement.IncomeMovementId = null;
                    }

                    await _investmentMovementRepository.AddAsync(investmentMovement);

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

            var (movements, totalCount) = await _investmentMovementRepository.GetPaginatedInvestmentMovements(userId, page, pageSize, "Crypto");

            var movementsDTO = movements.Select(m => new CryptoTransactionListDTO
            {
                Id = m.Id,
                Date = m.Date,
                MovementType = m.MovementType,
                CommerceType = m.CommerceType,
                ExpenseAsset = m.ExpenseMovement?.Asset?.Name,
                ExpenseAccount = m.ExpenseMovement?.Account?.Name,
                ExpenseAmount = m.ExpenseMovement?.Amount,
                ExpenseQuote = m.ExpenseMovement?.QuotePrice,
                IncomeAsset = m.IncomeMovement?.Asset?.Name,
                IncomeAccount = m.IncomeMovement?.Account?.Name,
                IncomeAmount = m.IncomeMovement?.Amount,
                IncomeQuote = m.IncomeMovement?.QuotePrice
            }).ToList();


            //if (Asset != "Peso Argentino") then quote = 1/quote
            foreach (var movement in movementsDTO)
            {
                if (movement.ExpenseAsset != null && movement.ExpenseAsset != "Peso Argentino")
                {
                    movement.ExpenseQuote = 1 / movement.ExpenseQuote.Value;
                }
                if (movement.IncomeAsset != null && movement.IncomeAsset != "Peso Argentino")
                {
                    movement.IncomeQuote = 1 / movement.IncomeQuote.Value;
                }
            }

            return Ok(new { movements = movementsDTO, totalCount });


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

        //    var movement = await _investmentMovementRepository.GetInvestmentMovementById(id);

        //    if (movement == null)
        //    {
        //        return NotFound();
        //    }

        //    if (movement.UserId != userId)
        //    {
        //        return Unauthorized();
        //    }

        //    var movementDTO = new CryptoTransactionListDTO
        //    {
        //        Id = movement.Id,
        //        Date = movement.Date,
        //        MovementType = movement.MovementType,
        //        CommerceType = movement.CommerceType,
        //        ExpenseAsset = movement.ExpenseMovement?.Asset?.Name,
        //        ExpenseAccount = movement.ExpenseMovement?.Account?.Name,
        //        ExpenseAmount = movement.ExpenseMovement?.Amount,
        //        ExpenseQuote = movement.ExpenseMovement?.QuotePrice,
        //        IncomeAsset = movement.IncomeMovement?.Asset?.Name,
        //        IncomeAccount = movement.IncomeMovement?.Account?.Name,
        //        IncomeAmount = movement.IncomeMovement?.Amount,
        //        IncomeQuote = movement.IncomeMovement?.QuotePrice
        //    };

        //    return Ok(movementDTO);
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

            var movement = await _investmentMovementRepository.GetInvestmentMovementById(id);

            if (movement == null)
            {
                return NotFound();
            }

            if (movement.UserId != userId)
            {
                return Unauthorized();
            }

            
            
            try
            {
                await _investmentMovementRepository.BeginTransactionAsync();

                if (movement.IncomeMovement != null)
                {
                    await _movementRepository.DeleteAsync(movement.IncomeMovement.Id);
                }

                if (movement.ExpenseMovement != null)
                {
                    await _movementRepository.DeleteAsync(movement.ExpenseMovement.Id);
                }

                await _investmentMovementRepository.DeleteAsync(movement.Id);

                await _investmentMovementRepository.CommitTransactionAsync();

                return Ok();
            }
            catch
            {
                await _investmentMovementRepository.RollbackTransactionAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, "Database failure");
            }
                            
        }
    }
}
