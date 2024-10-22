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
        private readonly IMovementClassRepository _movementClassRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        public MovementController(IMovementRepository movementRepository, IAssetRepository assetRepository, 
            IAccountRepository accountRepository, IMovementClassRepository movementClassRepository,
            IAssetQuoteRepository assetQuoteRepository)
        {
            _movementRepository = movementRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _movementClassRepository = movementClassRepository;
            _assetQuoteRepository = assetQuoteRepository;
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

            var movementsDTO = movements.Select(m => new movementListDTO
            {
                Id = m.Id,
                Date = m.Date,
                Amount = m.Amount,
                Detail = m.Detail,
                AccountId = m.AccountId,
                AccountName = m.Account.Name,
                AssetId = m.AssetId,
                AssetName = m.Asset.Name,
                MovementClassId = m.MovementClassId,
                MovementClassName = m.MovementClass.Description,
                MovementType = m.MovementType

            }).ToList();

            return Ok(new { Movements = movementsDTO, TotalCount = totalCount });
        }

        [HttpPost]
         public async Task<IActionResult> CreateMovement(movementAddDTO movementDTO)
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


            if(asset.Symbol == "USD")
            {
                quotePrice = 1;
            } else if (movementDTO.quotePrice != 0)
            {
                if (asset.Symbol == "ARS")
                {
                    quotePrice = movementDTO.quotePrice;
                }
                else
                {
                    quotePrice = 1 / movementDTO.quotePrice;
                }
            } else
            {
                string type;
                if(asset.Symbol == "ARS")
                {
                    type = "BLUE";
                } else
                {
                    type = "NA";
                }

                quotePrice = await _assetQuoteRepository.GetQuotePrice(asset.Id, movementDTO.date, type);
            }
            

            var id = await _movementRepository.GetNextId();
            

            if (movementDTO.movementType == "I")
            {
                var incomeAccount = await _accountRepository.GetByIdAsync(movementDTO.incomeAccountId.Value);
                if (incomeAccount == null)
                {
                    return NotFound();
                }
                if(incomeAccount.UserId != userId)
                {
                    return Unauthorized();
                }
                var movementClass = await _movementClassRepository.GetByIdAsync(movementDTO.movementClassId.Value);
                if (movementClass == null)
                {
                    return NotFound();
                }
                if(movementClass.IncExp == "E")
                {
                    return BadRequest("No se puede asignar una clase de movimiento de tipo egreso a un movimiento de tipo ingreso");
                }
                if (movementClass.UserId != userId)
                {
                    return Unauthorized();
                }

                var movement = new Movement
                {
                    Id = id,
                    AccountId = incomeAccount.Id,
                    Account = incomeAccount,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = movementDTO.date,
                    MovementType = movementDTO.movementType,
                    MovementClassId = movementClass.Id,
                    MovementClass = movementClass,
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
                if(expenseAccount.UserId != userId)
                {
                    return Unauthorized();
                }

                var movementClass = await _movementClassRepository.GetByIdAsync(movementDTO.movementClassId.Value);
                if (movementClass == null)
                {
                    return NotFound();
                }
                if (movementClass.IncExp == "I")
                {
                    return BadRequest("No se puede asignar una clase de movimiento de tipo ingreso a un movimiento de tipo egreso");
                }
                if(movementClass.UserId != userId)
                {
                    return Unauthorized();
                }

                var movement = new Movement
                {
                    Id = id,
                    AccountId = expenseAccount.Id,
                    Account = expenseAccount,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = movementDTO.date,
                    MovementType = movementDTO.movementType,
                    MovementClassId = movementClass.Id,
                    MovementClass = movementClass,
                    Detail = movementDTO.detail,
                    Amount = - movementDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice
                };
                await _movementRepository.AddAsync(movement);
            }
            else if(movementDTO.movementType == "EX")
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
                    Id = id,
                    AccountId = incomeAccount.Id,
                    Account = incomeAccount,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = movementDTO.date,
                    MovementType = movementDTO.movementType,
                    MovementClassId = null,
                    Detail = movementDTO.detail,
                    Amount = movementDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice
                };

                await _movementRepository.AddAsync(movement);

                

                movement = new Movement
                {
                    Id = id,
                    AccountId = expenseAccount.Id,
                    Account = expenseAccount,
                    AssetId = asset.Id,
                    Asset = asset,
                    Date = movementDTO.date,
                    MovementType = movementDTO.movementType,
                    MovementClassId = null,
                    Detail = movementDTO.detail,
                    Amount = -movementDTO.amount,
                    UserId = userId,
                    QuotePrice = quotePrice
                };
                await _movementRepository.AddAsync(movement);
            }



            return Ok();
        }
       
    }
}
