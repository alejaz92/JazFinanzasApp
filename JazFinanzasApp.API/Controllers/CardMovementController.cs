using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.CardMovement;
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
        

        public CardMovementController(ICardMovementRepository cardMovementRepository,
            ICardRepository cardRepository,
            IAsset_UserRepository asset_UserRepository,
            IMovementClassRepository movementClassRepository,
            IAssetRepository assetRepository,
            IAssetQuoteRepository assetQuoteRepository,
            ICardPaymentRepository cardPaymentRepository)
        {
            _cardMovementRepository = cardMovementRepository;
            _cardRepository = cardRepository;
            _assetUserRepository = asset_UserRepository;
            _movementClassRepository = movementClassRepository;
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _cardPaymentRepository = cardPaymentRepository;
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

        [HttpGet("pending")]
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

            

            return Ok(cardPayments);
        }

        
    }
}
