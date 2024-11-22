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
    public class FiatCurrencyExchangeController : ControllerBase
    {
        private readonly IMovementRepository _movementRepository;
        private readonly IInvestmentMovementRepository _investmentMovementRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;

        public FiatCurrencyExchangeController(IMovementRepository movementRepository, IInvestmentMovementRepository investmentMovementRepository, IAssetRepository assetRepository, IAccountRepository accountRepository, IAssetQuoteRepository assetQuoteRepository)
        {
            _movementRepository = movementRepository;
            _investmentMovementRepository = investmentMovementRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _assetQuoteRepository = assetQuoteRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaginatedExchangeTransactions(int page = 1, int pageSize = 10, string environment = "CurrencyExchange")
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            try
            {
                var (movements, totalCount) = await _investmentMovementRepository.GetPaginatedInvestmentMovements(userId, page, pageSize, environment);

                var transactions = movements.Select(m => new CurrencyExchangeListDTO
                {
                    Id = m.Id,
                    Date = m.Date,
                    ExpenseAsset = m.ExpenseMovement?.Asset?.Name,
                    ExpenseAccount = m.ExpenseMovement?.Account?.Name,
                    ExpenseAmount = m.ExpenseMovement?.Amount,
                    IncomeAsset = m.IncomeMovement?.Asset?.Name,
                    IncomeAccount = m.IncomeMovement?.Account?.Name,
                    IncomeAmount = m.IncomeMovement?.Amount
                });

                return Ok(new { transactions, totalCount });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");

            }

        }

        [HttpPost]
        public async Task<IActionResult> CreateExchangeTransaction(CurrencyExchangeAddDTO exchangeTransactionDTO)
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

                // assign assets and accounts
                var expenseAsset = await _assetRepository.GetByIdAsync(exchangeTransactionDTO.ExpenseAssetId.Value);     
                var expenseAccount = await _accountRepository.GetByIdAsync(exchangeTransactionDTO.ExpenseAccountId.Value);              


                var incomeAsset = await _assetRepository.GetByIdAsync(exchangeTransactionDTO.IncomeAssetId.Value);             
                var incomeAccount = await _accountRepository.GetByIdAsync(exchangeTransactionDTO.IncomeAccountId.Value);

                if (expenseAsset == null || expenseAccount == null || incomeAsset == null || incomeAccount == null)
                {
                    return BadRequest("Invalid Asset or Account");
                }


                //assign quotes
                string assetQuoteType = null;
                if (expenseAsset.Name == "Peso Argentino")
                {
                    assetQuoteType = "BLUE";
                }
                var expenseQuote = await _assetQuoteRepository.GetLastQuoteByAsset(expenseAsset.Id, assetQuoteType);

                assetQuoteType = null;
                if (incomeAsset.Name == "Peso Argentino")
                {
                    assetQuoteType = "BLUE";
                }
                var incomeQuote = await _assetQuoteRepository.GetLastQuoteByAsset(incomeAsset.Id, assetQuoteType);


                //create movements
                var expenseMovement = new Movement
                {
                    AccountId = exchangeTransactionDTO.ExpenseAccountId.Value,
                    Account = expenseAccount,
                    AssetId = exchangeTransactionDTO.ExpenseAssetId.Value,
                    Asset = expenseAsset,
                    Date = exchangeTransactionDTO.Date,
                    MovementType = "E",
                    MovementClassId = null,
                    Detail = "Currency Exchange",
                    Amount = -exchangeTransactionDTO.ExpenseAmount.Value,
                    QuotePrice = expenseQuote.Value,
                    UserId = userId
                };

                var incomeMovement = new Movement
                {
                    AccountId = exchangeTransactionDTO.IncomeAccountId.Value,
                    Account = incomeAccount,
                    AssetId = exchangeTransactionDTO.IncomeAssetId.Value,
                    Asset = incomeAsset,
                    Date = exchangeTransactionDTO.Date,
                    MovementType = "I",
                    MovementClassId = null,
                    Detail = "Currency Exchange",
                    Amount = exchangeTransactionDTO.IncomeAmount.Value,
                    QuotePrice = incomeQuote.Value,
                    UserId = userId
                };

                //save movements
                expenseMovement = await _movementRepository.AddAsyncReturnObject(expenseMovement);
                expenseId = expenseMovement.Id;

                incomeMovement = await _movementRepository.AddAsyncReturnObject(incomeMovement);
                incomeId = incomeMovement.Id;


                //create and save investment movement
                var investmentMovement = new InvestmentMovement
                {
                    Date = exchangeTransactionDTO.Date,
                    Environment = "CurrencyExchange",
                    MovementType = "EX",  
                    CommerceType = "CurrencyExchange",
                    ExpenseMovementId = expenseId,
                    IncomeMovementId = incomeId,
                    UserId = userId
                };

                await _investmentMovementRepository.AddAsync(investmentMovement);
                return Ok();

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }
        }


        [HttpDelete("{id}")]
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
                var investmentMovement = await _investmentMovementRepository.GetInvestmentMovementById(id);

                if (investmentMovement == null)
                {
                    return NotFound();
                }

                if (investmentMovement.UserId != userId)
                {
                    return Unauthorized();
                }


                try
                {
                    await _investmentMovementRepository.BeginTransactionAsync();

                    if (investmentMovement.IncomeMovement != null)
                    {
                        await _movementRepository.DeleteAsync(investmentMovement.IncomeMovementId.Value);
                    }

                    if (investmentMovement.ExpenseMovement != null)
                    {
                        await _movementRepository.DeleteAsync(investmentMovement.ExpenseMovementId.Value);
                    }

                    await _investmentMovementRepository.DeleteAsync(investmentMovement.Id);
                    await _investmentMovementRepository.CommitTransactionAsync();
                    return Ok();
                }
                catch
                {
                    await _investmentMovementRepository.RollbackTransactionAsync();
                    return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
                }
                
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }
        }
    }
}
