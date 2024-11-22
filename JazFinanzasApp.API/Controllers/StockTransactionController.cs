using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.InvestmentMovement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockTransactionController : ControllerBase
    {
        private readonly IMovementRepository _movementRepository;
        private readonly IInvestmentMovementRepository _investmentMovementRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IMovementClassRepository _movementClassRepository;

        public StockTransactionController(
            IMovementRepository movementRepository,
            IInvestmentMovementRepository investmentMovementRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IMovementClassRepository movementClassRepository
            )
        {
            _movementRepository = movementRepository;
            _investmentMovementRepository = investmentMovementRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _movementClassRepository = movementClassRepository;
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
                    if (stockTransactionDto.StockTransactionType == "I")
                    {
                        var incomeAsset = await _assetRepository.GetByIdAsync(stockTransactionDto.IncomeAssetId.Value);
                        var incomeAccount = await _accountRepository.GetByIdAsync(stockTransactionDto.IncomeAccountId.Value);
                        await CheckAssetsAndAccounts(incomeAsset, incomeAccount);

                        var incomeMovement = new Movement
                        {
                            AccountId = stockTransactionDto.IncomeAccountId.Value,
                            Account = incomeAccount,
                            AssetId = stockTransactionDto.IncomeAssetId.Value,
                            Asset = incomeAsset,
                            Date = stockTransactionDto.Date,
                            MovementType = "I",
                            MovementClassId = null,
                            Detail = stockTransactionDto.CommerceType,
                            Amount = stockTransactionDto.IncomeQuantity.Value,
                            QuotePrice = 1 / stockTransactionDto.IncomeQuotePrice.Value,
                            UserId = userId
                        };

                        incomeMovement = await _movementRepository.AddAsyncReturnObject(incomeMovement);
                        incomeId = incomeMovement.Id;

                        if (stockTransactionDto.CommerceType == "General")
                        {
                            var expenseAsset = await _assetRepository.GetByIdAsync(stockTransactionDto.ExpenseAssetId.Value);
                            var expenseAccount = await _accountRepository.GetByIdAsync(stockTransactionDto.ExpenseAccountId.Value);
                            await CheckAssetsAndAccounts(expenseAsset, expenseAccount);

                            MovementClass investmentClass = await _movementClassRepository.GetMovementClassByDescriptionAsync("Inversiones");
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

                            var expenseMovement = new Movement
                            {
                                AccountId = stockTransactionDto.ExpenseAccountId.Value,
                                Account = expenseAccount,
                                AssetId = stockTransactionDto.ExpenseAssetId.Value,
                                Asset = expenseAsset,
                                Date = stockTransactionDto.Date,
                                MovementType = "E",
                                MovementClassId = investmentClass.Id,
                                Detail = stockTransactionDto.CommerceType,
                                Amount = -stockTransactionDto.ExpenseQuantity.Value,
                                QuotePrice = quote.Value,
                                UserId = userId
                            };

                            expenseMovement = await _movementRepository.AddAsyncReturnObject(expenseMovement);
                            expenseId = expenseMovement.Id;
                        }
                    }
                    else if (stockTransactionDto.StockTransactionType == "E")
                    {
                        var expenseAsset = await _assetRepository.GetByIdAsync(stockTransactionDto.ExpenseAssetId.Value);
                        var expenseAccount = await _accountRepository.GetByIdAsync(stockTransactionDto.ExpenseAccountId.Value);
                        await CheckAssetsAndAccounts(expenseAsset, expenseAccount);

                        var expenseMovement = new Movement
                        {
                            AccountId = stockTransactionDto.ExpenseAccountId.Value,
                            Account = expenseAccount,
                            AssetId = stockTransactionDto.ExpenseAssetId.Value,
                            Asset = expenseAsset,
                            Date = stockTransactionDto.Date,
                            MovementType = "E",
                            MovementClassId = null,
                            Detail = stockTransactionDto.CommerceType,
                            Amount = -stockTransactionDto.ExpenseQuantity.Value,
                            QuotePrice = stockTransactionDto.ExpenseQuotePrice.Value,
                            UserId = userId
                        };

                        expenseMovement = await _movementRepository.AddAsyncReturnObject(expenseMovement);
                        expenseId = expenseMovement.Id;

                        if (stockTransactionDto.CommerceType == "General")
                        {
                            var incomeAsset = await _assetRepository.GetByIdAsync(stockTransactionDto.IncomeAssetId.Value);
                            var incomeAccount = await _accountRepository.GetByIdAsync(stockTransactionDto.IncomeAccountId.Value);
                            await CheckAssetsAndAccounts(incomeAsset, incomeAccount);

                            MovementClass investmentClass = await _movementClassRepository.GetMovementClassByDescriptionAsync("Inversiones");
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

                            var incomeMovement = new Movement
                            {
                                AccountId = stockTransactionDto.IncomeAccountId.Value,
                                Account = incomeAccount,
                                AssetId = stockTransactionDto.IncomeAssetId.Value,
                                Asset = incomeAsset,
                                Date = stockTransactionDto.Date,
                                MovementType = "I",
                                MovementClassId = investmentClass.Id,
                                Detail = stockTransactionDto.CommerceType,
                                Amount = stockTransactionDto.IncomeQuantity.Value,
                                QuotePrice = quote.Value,
                                UserId = userId
                            };

                            incomeMovement = await _movementRepository.AddAsyncReturnObject(incomeMovement);
                            incomeId = incomeMovement.Id;
                        }
                    }

                    var investmentMovement = new InvestmentMovement
                    {
                        Date = stockTransactionDto.Date,
                        Environment = stockTransactionDto.Environment,
                        MovementType = stockTransactionDto.StockTransactionType,
                        CommerceType = stockTransactionDto.CommerceType,
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
                var (movements, totalCount) = await _investmentMovementRepository.GetPaginatedInvestmentMovements(userId, page, pageSize, environment);

                var transactions = movements.Select(m => new StockTransactionListDTO
                {
                    Id = m.Id,
                    Date = m.Date,
                    StockTransactionType = m.MovementType,
                    CommerceType = m.CommerceType,
                    AssetType =
                        m.ExpenseMovement == null || m.ExpenseMovement.Asset.AssetType.Name == "Moneda"
                        ? m.IncomeMovement.Asset.AssetType.Name
                        : m.ExpenseMovement.Asset.AssetType.Name,
                    ExpenseAsset = m.ExpenseMovement?.Asset?.Name,
                    ExpenseAccount = m.ExpenseMovement?.Account?.Name,
                    ExpenseQuantity = m.ExpenseMovement?.Amount,
                    ExpenseQuotePrice = m.ExpenseMovement?.Asset.Name == "Peso Argentino"
                        ? m.ExpenseMovement?.QuotePrice
                        : 1 / m.ExpenseMovement?.QuotePrice,
                    IncomeAsset = m.IncomeMovement?.Asset?.Name,
                    IncomeAccount = m.IncomeMovement?.Account?.Name,
                    IncomeQuantity = m.IncomeMovement?.Amount,
                    IncomeQuotePrice = m.IncomeMovement?.Asset.Name == "Peso Argentino"
                        ? m.IncomeMovement?.QuotePrice
                        : 1/m.IncomeMovement?.QuotePrice
                });

                

                return Ok(new { transactions, totalCount });
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
