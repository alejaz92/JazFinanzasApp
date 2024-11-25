using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Movement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MovementController : ControllerBase
    {
        private readonly IMovementRepository _movementRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IInvestmentMovementRepository _investmentMovementRepository;
        public MovementController(
            IMovementRepository movementRepository, 
            IAssetRepository assetRepository,
            IAccountRepository accountRepository, 
            ITransactionClassRepository transactionClassRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IInvestmentMovementRepository investmentMovementRepository
            )
        {
            _movementRepository = movementRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _transactionClassRepository = transactionClassRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _investmentMovementRepository = investmentMovementRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaginatedMovements([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            
            var (movements, totalCount) = await _movementRepository.GetPaginatedMovements(userId, page, pageSize);



            var movementsDTO = movements.Select(m => new MovementListDTO
            {
                Id = m.Id,
                Date = m.Date,
                Amount = m.Amount,
                Detail = m.Detail,
                AccountId = m.AccountId,
                AccountName = m.Account.Name,
                AssetId = m.AssetId,
                AssetName = m.Asset.Name,
                TransactionClassId = m.TransactionClassId,
                TransactionClassName = m.TransactionClass.Description,
                MovementType = m.MovementType

            }).ToList();

            return Ok(new { Movements = movementsDTO, TotalCount = totalCount });
        }

        [HttpPost]
        public async Task<IActionResult> CreateMovement(MovementAddDTO movementDTO)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);


            var asset = await _assetRepository.GetByIdAsync(movementDTO.assetId);
            if (asset == null)
            {
                return NotFound();
            }
            decimal quotePrice = 0;


            if (asset.Symbol == "USD")
            {
                quotePrice = 1;
            }
            else if (movementDTO.quotePrice != 0)
            {
                if (asset.Symbol == "ARS")
                {
                    quotePrice = movementDTO.quotePrice;
                }
                else
                {
                    quotePrice = 1 / movementDTO.quotePrice;
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

                quotePrice = await _assetQuoteRepository.GetQuotePrice(asset.Id, movementDTO.date, type);
            }


           


            if (movementDTO.movementType == "I")
            {
                var incomeAccount = await _accountRepository.GetByIdAsync(movementDTO.incomeAccountId.Value);
                if (incomeAccount == null)
                {
                    return NotFound();
                }
                if (incomeAccount.UserId != userId)
                {
                    return Unauthorized();
                }
                var transactionClass = await _transactionClassRepository.GetByIdAsync(movementDTO.transactionClassId.Value);
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

                var movement = new Movement
                {
                    AccountId = incomeAccount.Id,
                    Account = incomeAccount,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = movementDTO.date,
                    MovementType = movementDTO.movementType,
                    TransactionClassId = transactionClass.Id,
                    TransactionClass = transactionClass,
                    Detail = movementDTO.detail,
                    Amount = movementDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice
                };

                await _movementRepository.AddAsync(movement);

            }
            else if (movementDTO.movementType == "E")
            {
                var expenseAccount = await _accountRepository.GetByIdAsync(movementDTO.expenseAccountId.Value);
                if (expenseAccount == null)
                {
                    return NotFound();
                }
                if (expenseAccount.UserId != userId)
                {
                    return Unauthorized();
                }

                var transactionClass = await _transactionClassRepository.GetByIdAsync(movementDTO.transactionClassId.Value);
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

                var movement = new Movement
                {
                    AccountId = expenseAccount.Id,
                    Account = expenseAccount,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = movementDTO.date,
                    MovementType = movementDTO.movementType,
                    TransactionClassId = transactionClass.Id,
                    TransactionClass = transactionClass,
                    Detail = movementDTO.detail,
                    Amount = -movementDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice
                };
                await _movementRepository.AddAsync(movement);
            }
            else if (movementDTO.movementType == "EX")
            {
                var time = DateTime.UtcNow;

                var incomeAccount = await _accountRepository.GetByIdAsync(movementDTO.incomeAccountId.Value);
                if (incomeAccount == null)
                {
                    return NotFound();
                }
                if (incomeAccount.UserId != userId)
                {
                    return Unauthorized();
                }

                var expenseAccount = await _accountRepository.GetByIdAsync(movementDTO.expenseAccountId.Value);
                if (expenseAccount == null)
                {
                    return NotFound();
                }
                if (expenseAccount.UserId != userId)
                {
                    return Unauthorized();
                }

                var movement = new Movement
                {
                    AccountId = incomeAccount.Id,
                    Account = incomeAccount,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = movementDTO.date,
                    MovementType = movementDTO.movementType,
                    TransactionClassId = null,
                    Detail = movementDTO.detail,
                    Amount = movementDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice,
                    CreatedAt = time,
                    UpdatedAt = time
                };

                await _movementRepository.AddAsync(movement);



                movement = new Movement
                {
                    AccountId = expenseAccount.Id,
                    Account = expenseAccount,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = movementDTO.date,
                    MovementType = movementDTO.movementType,
                    TransactionClassId = null,
                    Detail = movementDTO.detail,
                    Amount = -movementDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice,
                    CreatedAt = time,
                    UpdatedAt = time
                };
                await _movementRepository.AddAsync(movement);
            }



            return Ok();
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> EditMovement(int id, MovementEditDTO movementDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var movement = await _movementRepository.GetByIdAsync(id);
            if (movement == null)
            {
                return NotFound();
            }
            if (movement.UserId != userId)
            {
                return Unauthorized();
            }

            movement.Amount = movementDTO.Amount;
            if (movement.MovementType == "E" && movement.Amount > 0)
            {
                movement.Amount = -movementDTO.Amount;
            }
            movement.UpdatedAt = DateTime.UtcNow;

            await _movementRepository.UpdateAsync(movement);

            return Ok();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovement(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var movement = await _movementRepository.GetByIdAsync(id);

            if (movement == null)
            {
                return NotFound();
            }
            if (movement.UserId != userId)
            {
                return Unauthorized();
            }

            await _movementRepository.DeleteAsync(movement.Id);

            return Ok();
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetMovement(int Id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var movement = await _movementRepository.GetMovementByIdAsync(Id);
            if (movement == null)
            {
                return NotFound();
            }
            if (movement.UserId != userId)
            {
                return Unauthorized();
            }

            var movementDTO = new MovementListDTO
            {
                Id = movement.Id,
                Date = movement.Date,
                Amount = movement.Amount,
                Detail = movement.Detail,
                AccountId = movement.AccountId,
                AccountName = movement.Account.Name,
                AssetId = movement.AssetId,
                AssetName = movement.Asset.Name,
                TransactionClassId = movement.TransactionClassId,
                TransactionClassName = movement.TransactionClass.Description,
                MovementType = movement.MovementType
            };

            return Ok(movementDTO);
        }

        [HttpPost("refund/{Id}")]
        public async Task<IActionResult> RefundMovement(int Id, [FromBody] RefundDTO refundDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            var movement = await _movementRepository.GetByIdAsync(Id);
            if (movement == null)
            {
                return NotFound();
            }
            if (movement.UserId != userId)
            {
                return Unauthorized();
            }

            if (movement.MovementType == "I")
            {
                return BadRequest("Cannot refund an income movement");
            }

            var refundAccount = await _accountRepository.GetByIdAsync(refundDTO.AccountId);
            if (refundAccount == null)
            {
                return NotFound();
            }



            movement.Amount = movement.Amount + refundDTO.Amount;
            movement.UpdatedAt = DateTime.UtcNow;
            await _movementRepository.UpdateAsync(movement);

            if (movement.AccountId != refundAccount.Id)
            {
                var time = DateTime.UtcNow;

                var refundExpenseMovement = new Movement
                {
                    AccountId = movement.AccountId,
                    Account = movement.Account,
                    AssetId = movement.AssetId,
                    Asset = movement.Asset,
                    Date = refundDTO.Date,
                    MovementType = "EX",
                    TransactionClassId = null,
                    Detail = "Refund",
                    Amount = - refundDTO.Amount,
                    UserId = userId,
                    CreatedAt = time,
                    UpdatedAt = time,
                    QuotePrice = movement.QuotePrice                    
                };
                await _movementRepository.AddAsync(refundExpenseMovement);

                var refundIncomeMovement = new Movement
                {
                    AccountId = refundAccount.Id,
                    Account = refundAccount,
                    AssetId = movement.AssetId,
                    Asset = movement.Asset,
                    Date = refundDTO.Date,
                    MovementType = "EX",
                    TransactionClassId = null,
                    Detail = "Refund",
                    Amount = refundDTO.Amount,
                    UserId = userId,
                    CreatedAt = time,
                    UpdatedAt = time,
                    QuotePrice = movement.QuotePrice
                };
                await _movementRepository.AddAsync(refundIncomeMovement);
            }
            return Ok();
        }

    }
}
