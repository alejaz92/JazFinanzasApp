using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.CardMovement;
using JazFinanzasApp.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CardMovementController : ControllerBase
    {
        private readonly ICardMovementRepository _cardMovementRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IAsset_UserRepository _assetUserRepository;
        private readonly IMovementClassRepository _movementClassRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly ICardPaymentRepository _cardPaymentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAccount_AssetTypeRepository _account_AssetTypeRepository;
        private readonly IMovementRepository _movementRepository;


        public CardMovementController(ICardMovementRepository cardMovementRepository,
            ICardRepository cardRepository,
            IAsset_UserRepository asset_UserRepository,
            IMovementClassRepository movementClassRepository,
            IAssetRepository assetRepository,
            IAssetQuoteRepository assetQuoteRepository,
            ICardPaymentRepository cardPaymentRepository,
            IAccountRepository accountRepository,
            IAccount_AssetTypeRepository account_AssetTypeRepository,
            IMovementRepository movementRepository)
        {
            _cardMovementRepository = cardMovementRepository;
            _cardRepository = cardRepository;
            _assetUserRepository = asset_UserRepository;
            _movementClassRepository = movementClassRepository;
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _cardPaymentRepository = cardPaymentRepository;
            _accountRepository = accountRepository;
            _account_AssetTypeRepository = account_AssetTypeRepository;
            _movementRepository = movementRepository;
        }



        [HttpPost]
        public async Task<IActionResult> AddCardMovement([FromBody] CardMovementAddDTO cardMovementAddDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var card = await _cardRepository.GetByIdAsync(cardMovementAddDTO.CardId);
            if (card == null)
            {
                return BadRequest("Card not found");
            }

            var asset = await _assetRepository.GetByIdAsync(cardMovementAddDTO.AssetId);
            if (asset == null)
            {
                return BadRequest("Asset not found");
            }

            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardMovementAddDTO.AssetId);
            if (assetUser == null)
            {
                return Unauthorized();
            }

            var movementClass = await _movementClassRepository.GetByIdAsync(cardMovementAddDTO.MovementClassId);
            if (movementClass == null)
            {
                return BadRequest("Movement class not found");
            }

            //make first installment and last installment the day 1 of its month
            cardMovementAddDTO.FirstInstallment = new DateTime(cardMovementAddDTO.FirstInstallment.Year, cardMovementAddDTO.FirstInstallment.Month, 1);
            cardMovementAddDTO.LastInstallment = new DateTime(cardMovementAddDTO.LastInstallment.Year, cardMovementAddDTO.LastInstallment.Month, 1);

            var cardMovement = new CardMovement
            {
                Date = cardMovementAddDTO.Date,
                Detail = cardMovementAddDTO.Detail,
                CardId = cardMovementAddDTO.CardId,
                Card = card,
                MovementClassId = cardMovementAddDTO.MovementClassId,
                MovementClass = movementClass,
                AssetId = cardMovementAddDTO.AssetId,
                Asset = asset,
                TotalAmount = cardMovementAddDTO.TotalAmount,
                Installments = cardMovementAddDTO.Installments,
                FirstInstallment = cardMovementAddDTO.FirstInstallment,
                LastInstallment = cardMovementAddDTO.LastInstallment,
                Repeat = cardMovementAddDTO.Repeat,
                UserId = userId,
                InstallmentAmount = cardMovementAddDTO.TotalAmount / cardMovementAddDTO.Installments
            };

            await _cardMovementRepository.AddAsync(cardMovement);

            return Ok(cardMovementAddDTO);

        }

        [HttpGet]
        public async Task<IActionResult> GetPendingCardMovements()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var pendingMovements = await _cardMovementRepository.GetPendingCardMovementsAsync(userId);

            return Ok(pendingMovements);
        }

        [HttpGet("CardPayments")]
        public async Task<IActionResult> GetCardPayments(int CardId, DateTime paymentMonth)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var card = await _cardRepository.GetByIdAsync(CardId);
            if (card == null)
            {
                return BadRequest("Card not found");
            }

            var today = DateTime.Today;

            var isPaymentMade = await _cardPaymentRepository.IsPaymentAlreadyMadeAsync(CardId, paymentMonth);
            if (isPaymentMade)
            {
                return NotFound("Payment already made");
            }

            var peso = await _assetRepository.GetAssetByNameAsync("Peso Argentino");

            var exchangeRate = await _assetQuoteRepository.GetQuotePrice(peso.Id, today, "TARJETA");

            var cardMovements = await _cardMovementRepository.GetCardMovementsToPay(CardId, paymentMonth, userId);



            var cardPaymments = cardMovements.Select(m =>
            {
                // Calculamos el número de cuota actual solo para movimientos únicos
                string installmentDisplay;
                if (m.Repeat == "YES")
                {
                    installmentDisplay = "Recurrente";
                }
                else
                {
                    // Calculamos la cuota actual con la diferencia en meses entre paymentMonth y FirstInstallment
                    var currentInstallment = ((paymentMonth.Year - m.FirstInstallment.Year) * 12) + paymentMonth.Month - m.FirstInstallment.Month + 1;
                    installmentDisplay = $"{currentInstallment}/{m.Installments}";
                }

                // Calculamos el valor en pesos si el movimiento está en dólares
                var valueInPesos = m.Asset.Name == "Dolar Estadounidense" ? m.InstallmentAmount * exchangeRate : m.InstallmentAmount;

                return new CardMovementsPaymentListDTO
                {
                    Date = m.Date,
                    MovementClassId = m.MovementClassId,
                    MovementClass = m.MovementClass.Description,
                    Detail = m.Detail,
                    AssetId = m.AssetId,
                    Asset = m.Asset.Name,
                    Installment = installmentDisplay,
                    InstallmentAmount = m.InstallmentAmount,
                    ValueInPesos = valueInPesos
                };
            }).ToList();

            return Ok(cardPaymments);
        }

        [HttpPost("CardPayments")]
        public async Task<IActionResult> RegisterCardPayment([FromBody] CardMovementsPaymentDTO cardMovementsPaymentDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var card = await _cardRepository.GetByIdAsync(cardMovementsPaymentDTO.CardId);
            if (card == null) return BadRequest("Card not found");


            Account account = await _accountRepository.GetByIdAsync(cardMovementsPaymentDTO.accountId);
            if (account == null) return BadRequest("Account not found");


            Account_AssetType account_AssetType = await _account_AssetTypeRepository
                .GetAccount_AssetTypeByAccountIdAndAssetTypeNameAsync(account.Id, "Moneda");
            if (account_AssetType == null) return BadRequest("Account_AssetType not found");


            var peso = await _assetRepository.GetAssetByNameAsync("Peso Argentino");
            var dolar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
            var quotePrice = await _assetQuoteRepository
                .GetQuotePrice(peso.Id, cardMovementsPaymentDTO.PaymentDate, "BLUE");


            await _movementRepository.BeginTransactionAsync();

            try
            {
                foreach (var cardMovement in cardMovementsPaymentDTO.CardMovements)
                {



                    var asset = await _assetRepository.GetByIdAsync(cardMovement.AssetId);
                    if (asset == null || (asset.Name != "Peso Argentino" && asset.Name != "Dolar Estadounidense")) BadRequest("Error in Validation");
                    cardMovement.Asset = asset.Name;


                    var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardMovement.AssetId);
                    if (assetUser == null) return BadRequest("Error in Validation");

                    var movementClass = await _movementClassRepository.GetByIdAsync(cardMovement.MovementClassId);
                    if (movementClass == null) return BadRequest("Error in Validation");

                    cardMovement.MovementClass = movementClass.Description;



                    var movement = CreateMovement(cardMovementsPaymentDTO, cardMovement, userId, peso, dolar, quotePrice);

                    await _movementRepository.AddAsyncTransaction(movement);

                }

                var cardExpensesClass = await _movementClassRepository.GetMovementClassByDescriptionAsync("Gastos Tarjeta");
                if (cardExpensesClass == null) return BadRequest("Movement class 'Gastos Tarjeta' not found");


                Movement cardExpenses = new Movement
                {
                    Date = cardMovementsPaymentDTO.PaymentDate,
                    Detail = $"Gastos Tarjeta - {card.Name}",
                    AccountId = cardMovementsPaymentDTO.accountId,
                    MovementClassId = cardExpensesClass.Id,
                    MovementType = "E",
                    UserId = userId,
                    Amount = -cardMovementsPaymentDTO.CardExpenses,
                    AssetId = peso.Id,
                    QuotePrice = quotePrice

                };

                await _movementRepository.AddAsyncTransaction(cardExpenses);
                await _movementRepository.SaveChangesAsyncTransaction();
                await _movementRepository.CommitTransactionAsync();


                //guardo en tabla cardPayment

                CardPayment cardPayment = new CardPayment
                {
                    CardId = cardMovementsPaymentDTO.CardId,
                    Card = card,
                    Date = cardMovementsPaymentDTO.PaymentMonth
                };

                await _cardPaymentRepository.AddAsync(cardPayment);

                return Ok(cardMovementsPaymentDTO);
            }
            catch (Exception ex)
            {
                await _movementRepository.RollbackTransactionAsync();
                return BadRequest(ex.Message);
            }

        }

        private async Task<bool> ValidateMovement(CardMovementsPaymentListDTO cardMovement, int userId)
        {
            var asset = await _assetRepository.GetByIdAsync(cardMovement.AssetId);
            if (asset == null || (asset.Name != "Peso Argentino" && asset.Name != "Dolar Estadounidense")) return false;

            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardMovement.AssetId);
            if (assetUser == null) return false;

            var movementClass = await _movementClassRepository.GetByIdAsync(cardMovement.MovementClassId);
            return movementClass != null;
        }

        private Movement CreateMovement(CardMovementsPaymentDTO cardMovementspaymentDTO, CardMovementsPaymentListDTO cardMovementsPaymentListDTO, int userId, Asset peso, Asset dolar, decimal quotePrice)
        {
            var movement = new Movement
            {
                Date = cardMovementspaymentDTO.PaymentMonth,
                Detail = $"(Tarjeta | {cardMovementsPaymentListDTO.Installment}) {cardMovementsPaymentListDTO.Detail}",
                AccountId = cardMovementspaymentDTO.accountId,
                MovementClassId = cardMovementsPaymentListDTO.MovementClassId,
                MovementType = "E",
                UserId = userId,
                Amount = cardMovementsPaymentListDTO.Asset == "Dolar Estadounidense" && cardMovementspaymentDTO.PaymentAsset == "P+D" ?
                 -cardMovementsPaymentListDTO.InstallmentAmount : -cardMovementsPaymentListDTO.ValueInPesos,
                AssetId = cardMovementsPaymentListDTO.Asset == "Dolar Estadounidense" && cardMovementspaymentDTO.PaymentAsset == "P+D" ? dolar.Id : peso.Id,
                QuotePrice = quotePrice
            };

            movement.Asset = movement.AssetId == dolar.Id ? dolar : peso;
            return movement;
        }


        [HttpGet("EditRecurrent/{Id}")]
        public async Task<IActionResult> EditRecurrent(int Id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var cardMovement = await _cardMovementRepository.GetByIdAsync(Id);
            if (cardMovement == null) return BadRequest("Card movement not found");

            if (cardMovement.Repeat != "YES") return BadRequest("Card movement is not recurrent");

            if (cardMovement.UserId != userId) return Unauthorized();

            var card = await _cardRepository.GetByIdAsync(cardMovement.CardId);
            if (card == null) return BadRequest("Card not found");

            var asset = await _assetRepository.GetByIdAsync(cardMovement.AssetId);
            if (asset == null) return BadRequest("Asset not found");

            var movementClass = await _movementClassRepository.GetByIdAsync(cardMovement.MovementClassId);
            if (movementClass == null) return BadRequest("Movement class not found");

            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardMovement.AssetId);
            if (assetUser == null) return Unauthorized();

            var cardMovementDTO = new EditRecurrentListDTO
            {
                Id = cardMovement.Id,
                Date = cardMovement.Date,
                Card = card.Name,
                Description = cardMovement.Detail,
                Amount = cardMovement.InstallmentAmount,
                FirstInstallment = cardMovement.FirstInstallment
            };

            return Ok(cardMovementDTO);
        }

        [HttpPut("EditRecurrent/{Id}")]
        public async Task<IActionResult> EditRecurrent([FromBody] EditRecurrentDTO editRecurrentDTO, int Id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            
            var newFirstInstallment = new DateTime(editRecurrentDTO.newDate.Year, editRecurrentDTO.newDate.Month, 1);

            
            var oldLastInstallment = newFirstInstallment.AddMonths(-1);


            var cardMovement = await _cardMovementRepository.GetByIdAsync(Id);
            if (cardMovement == null) return BadRequest("Card movement not found");

            if (cardMovement.Repeat != "YES") return BadRequest("Card movement is not recurrent");

            if (cardMovement.UserId != userId) return Unauthorized();


            if (oldLastInstallment < cardMovement.FirstInstallment) return BadRequest("Date is lower than first installment");

            cardMovement.Repeat = "CLOSED";
            cardMovement.LastInstallment = oldLastInstallment;
            cardMovement.UpdatedAt = DateTime.UtcNow;


            await _cardMovementRepository.UpdateAsync(cardMovement);

            if (editRecurrentDTO.isUpdate)
            {
                // new cardmovement
                var newCardMovement = new CardMovement
                {
                    Date = editRecurrentDTO.newDate,
                    Detail = cardMovement.Detail,
                    CardId = cardMovement.CardId,
                    Card = cardMovement.Card,
                    MovementClassId = cardMovement.MovementClassId,
                    MovementClass = cardMovement.MovementClass,
                    AssetId = cardMovement.AssetId,
                    Asset = cardMovement.Asset,
                    TotalAmount = editRecurrentDTO.newAmount.Value,
                    Installments = cardMovement.Installments,
                    FirstInstallment = newFirstInstallment,
                    //LAST INST

                    Repeat = "YES",
                    UserId = userId,
                    InstallmentAmount = editRecurrentDTO.newAmount.Value
                };

                await _cardMovementRepository.AddAsync(newCardMovement);

            }
            return Ok();

        }
    }     
}
