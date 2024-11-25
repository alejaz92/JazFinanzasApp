using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.CardTransaction;
using JazFinanzasApp.API.Models.DTO.CardTransaction;
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
    public class CardTransactionController : ControllerBase
    {
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IAsset_UserRepository _assetUserRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly ICardPaymentRepository _cardPaymentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAccount_AssetTypeRepository _account_AssetTypeRepository;
        private readonly ITransactionRepository _transactionRepository;


        public CardTransactionController(ICardTransactionRepository cardTransactionRepository,
            ICardRepository cardRepository,
            IAsset_UserRepository asset_UserRepository,
            ITransactionClassRepository transactionClassRepository,
            IAssetRepository assetRepository,
            IAssetQuoteRepository assetQuoteRepository,
            ICardPaymentRepository cardPaymentRepository,
            IAccountRepository accountRepository,
            IAccount_AssetTypeRepository account_AssetTypeRepository,
            ITransactionRepository transactionRepository)
        {
            _cardTransactionRepository = cardTransactionRepository;
            _cardRepository = cardRepository;
            _assetUserRepository = asset_UserRepository;
            _transactionClassRepository = transactionClassRepository;
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _cardPaymentRepository = cardPaymentRepository;
            _accountRepository = accountRepository;
            _account_AssetTypeRepository = account_AssetTypeRepository;
            _transactionRepository = transactionRepository;
        }



        [HttpPost]
        public async Task<IActionResult> AddCardTransaction([FromBody] CardTransactionAddDTO cardTransactionAddDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var card = await _cardRepository.GetByIdAsync(cardTransactionAddDTO.CardId);
            if (card == null)
            {
                return BadRequest("Card not found");
            }

            var asset = await _assetRepository.GetByIdAsync(cardTransactionAddDTO.AssetId);
            if (asset == null)
            {
                return BadRequest("Asset not found");
            }

            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardTransactionAddDTO.AssetId);
            if (assetUser == null)
            {
                return Unauthorized();
            }

            var transactionClass = await _transactionClassRepository.GetByIdAsync(cardTransactionAddDTO.TransactionClassId);
            if (transactionClass == null)
            {
                return BadRequest("Transaction class not found");
            }

            //make first installment and last installment the day 1 of its month
            cardTransactionAddDTO.FirstInstallment = new DateTime(cardTransactionAddDTO.FirstInstallment.Year, cardTransactionAddDTO.FirstInstallment.Month, 1);
            cardTransactionAddDTO.LastInstallment = new DateTime(cardTransactionAddDTO.LastInstallment.Year, cardTransactionAddDTO.LastInstallment.Month, 1);

            var cardTransaction = new CardTransaction
            {
                Date = cardTransactionAddDTO.Date,
                Detail = cardTransactionAddDTO.Detail,
                CardId = cardTransactionAddDTO.CardId,
                Card = card,
                TransactionClassId = cardTransactionAddDTO.TransactionClassId,
                TransactionClass = transactionClass,
                AssetId = cardTransactionAddDTO.AssetId,
                Asset = asset,
                TotalAmount = cardTransactionAddDTO.TotalAmount,
                Installments = cardTransactionAddDTO.Installments,
                FirstInstallment = cardTransactionAddDTO.FirstInstallment,
                LastInstallment = cardTransactionAddDTO.LastInstallment,
                Repeat = cardTransactionAddDTO.Repeat,
                UserId = userId,
                InstallmentAmount = cardTransactionAddDTO.TotalAmount / cardTransactionAddDTO.Installments
            };

            await _cardTransactionRepository.AddAsync(cardTransaction);

            return Ok(cardTransactionAddDTO);

        }

        [HttpGet]
        public async Task<IActionResult> GetPendingCardTransactions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var pendingTransactions = await _cardTransactionRepository.GetPendingCardTransactionsAsync(userId);

            return Ok(pendingTransactions);
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

            var cardTransactions = await _cardTransactionRepository.GetCardTransactionsToPay(CardId, paymentMonth, userId);



            var cardPaymments = cardTransactions.Select(m =>
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

                return new CardTransactionPaymentListDTO
                {
                    Date = m.Date,
                    TransactionClassId = m.TransactionClassId,
                    TransactionClass = m.TransactionClass.Description,
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
        public async Task<IActionResult> RegisterCardPayment([FromBody] CardTransactionPaymentDTO cardTransactionsPaymentDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var card = await _cardRepository.GetByIdAsync(cardTransactionsPaymentDTO.CardId);
            if (card == null) return BadRequest("Card not found");


            Account account = await _accountRepository.GetByIdAsync(cardTransactionsPaymentDTO.accountId);
            if (account == null) return BadRequest("Account not found");


            Account_AssetType account_AssetType = await _account_AssetTypeRepository
                .GetAccount_AssetTypeByAccountIdAndAssetTypeNameAsync(account.Id, "Moneda");
            if (account_AssetType == null) return BadRequest("Account_AssetType not found");


            var peso = await _assetRepository.GetAssetByNameAsync("Peso Argentino");
            var dolar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
            var quotePrice = await _assetQuoteRepository
                .GetQuotePrice(peso.Id, cardTransactionsPaymentDTO.PaymentDate, "BLUE");


            await _transactionRepository.BeginTransactionAsync();

            try
            {
                foreach (var cardTransaction in cardTransactionsPaymentDTO.CardTransactions)
                {



                    var asset = await _assetRepository.GetByIdAsync(cardTransaction.AssetId);
                    if (asset == null || (asset.Name != "Peso Argentino" && asset.Name != "Dolar Estadounidense")) BadRequest("Error in Validation");
                    cardTransaction.Asset = asset.Name;


                    var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardTransaction.AssetId);
                    if (assetUser == null) return BadRequest("Error in Validation");

                    var transactionClass = await _transactionClassRepository.GetByIdAsync(cardTransaction.TransactionClassId);
                    if (transactionClass == null) return BadRequest("Error in Validation");

                    cardTransaction.TransactionClass = transactionClass.Description;



                    var transaction = CreateTransaction(cardTransactionsPaymentDTO, cardTransaction, userId, peso, dolar, quotePrice);

                    await _transactionRepository.AddAsyncTransaction(transaction);

                }

                var cardExpensesClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Gastos Tarjeta");
                if (cardExpensesClass == null) return BadRequest("Transaction class 'Gastos Tarjeta' not found");


                Transaction cardExpenses = new Transaction
                {
                    Date = cardTransactionsPaymentDTO.PaymentDate,
                    Detail = $"Gastos Tarjeta - {card.Name}",
                    AccountId = cardTransactionsPaymentDTO.accountId,
                    TransactionClassId = cardExpensesClass.Id,
                    MovementType = "E",
                    UserId = userId,
                    Amount = -cardTransactionsPaymentDTO.CardExpenses,
                    AssetId = peso.Id,
                    QuotePrice = quotePrice

                };

                await _transactionRepository.AddAsyncTransaction(cardExpenses);
                await _transactionRepository.CommitTransactionAsync();


                //guardo en tabla cardPayment

                CardPayment cardPayment = new CardPayment
                {
                    CardId = cardTransactionsPaymentDTO.CardId,
                    Card = card,
                    Date = cardTransactionsPaymentDTO.PaymentMonth
                };

                await _cardPaymentRepository.AddAsync(cardPayment);

                return Ok(cardTransactionsPaymentDTO);
            }
            catch (Exception ex)
            {
                await _transactionRepository.RollbackTransactionAsync();
                return BadRequest(ex.Message);
            }

        }

        private async Task<bool> ValidateTransaction(CardTransactionPaymentListDTO cardTransaction, int userId)
        {
            var asset = await _assetRepository.GetByIdAsync(cardTransaction.AssetId);
            if (asset == null || (asset.Name != "Peso Argentino" && asset.Name != "Dolar Estadounidense")) return false;

            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardTransaction.AssetId);
            if (assetUser == null) return false;

            var transactionClass = await _transactionClassRepository.GetByIdAsync(cardTransaction.TransactionClassId);
            return transactionClass != null;
        }

        private Transaction CreateTransaction(CardTransactionPaymentDTO cardTransactionspaymentDTO, CardTransactionPaymentListDTO cardTransactionsPaymentListDTO, int userId, Asset peso, Asset dolar, decimal quotePrice)
        {
            var transaction = new Transaction
            {
                Date = cardTransactionspaymentDTO.PaymentMonth,
                Detail = $"(Tarjeta | {cardTransactionsPaymentListDTO.Installment}) {cardTransactionsPaymentListDTO.Detail}",
                AccountId = cardTransactionspaymentDTO.accountId,
                TransactionClassId = cardTransactionsPaymentListDTO.TransactionClassId,
                MovementType = "E",
                UserId = userId,
                Amount = cardTransactionsPaymentListDTO.Asset == "Dolar Estadounidense" && cardTransactionspaymentDTO.PaymentAsset == "P+D" ?
                 -cardTransactionsPaymentListDTO.InstallmentAmount : -cardTransactionsPaymentListDTO.ValueInPesos,
                AssetId = cardTransactionsPaymentListDTO.Asset == "Dolar Estadounidense" && cardTransactionspaymentDTO.PaymentAsset == "P+D" ? dolar.Id : peso.Id,
                QuotePrice = quotePrice
            };

            transaction.Asset = transaction.AssetId == dolar.Id ? dolar : peso;
            return transaction;
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

            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(Id);
            if (cardTransaction == null) return BadRequest("Card transaction not found");

            if (cardTransaction.Repeat != "YES") return BadRequest("Card transaction is not recurrent");

            if (cardTransaction.UserId != userId) return Unauthorized();

            var card = await _cardRepository.GetByIdAsync(cardTransaction.CardId);
            if (card == null) return BadRequest("Card not found");

            var asset = await _assetRepository.GetByIdAsync(cardTransaction.AssetId);
            if (asset == null) return BadRequest("Asset not found");

            var transactionClass = await _transactionClassRepository.GetByIdAsync(cardTransaction.TransactionClassId);
            if (transactionClass == null) return BadRequest("Transaction class not found");

            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardTransaction.AssetId);
            if (assetUser == null) return Unauthorized();

            var cardTransactionDTO = new EditRecurrentListDTO
            {
                Id = cardTransaction.Id,
                Date = cardTransaction.Date,
                Card = card.Name,
                Description = cardTransaction.Detail,
                Amount = cardTransaction.InstallmentAmount,
                FirstInstallment = cardTransaction.FirstInstallment
            };

            return Ok(cardTransactionDTO);
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


            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(Id);
            if (cardTransaction == null) return BadRequest("Card transaction not found");

            if (cardTransaction.Repeat != "YES") return BadRequest("Card transaction is not recurrent");

            if (cardTransaction.UserId != userId) return Unauthorized();


            if (oldLastInstallment < cardTransaction.FirstInstallment) return BadRequest("Date is lower than first installment");

            cardTransaction.Repeat = "CLOSED";
            cardTransaction.LastInstallment = oldLastInstallment;
            cardTransaction.UpdatedAt = DateTime.UtcNow;


            await _cardTransactionRepository.UpdateAsync(cardTransaction);

            if (editRecurrentDTO.isUpdate)
            {
                // new cardtransaction
                var newCardTransaction = new CardTransaction
                {
                    Date = editRecurrentDTO.newDate,
                    Detail = cardTransaction.Detail,
                    CardId = cardTransaction.CardId,
                    Card = cardTransaction.Card,
                    TransactionClassId = cardTransaction.TransactionClassId,
                    TransactionClass = cardTransaction.TransactionClass,
                    AssetId = cardTransaction.AssetId,
                    Asset = cardTransaction.Asset,
                    TotalAmount = editRecurrentDTO.newAmount.Value,
                    Installments = cardTransaction.Installments,
                    FirstInstallment = newFirstInstallment,
                    //LAST INST

                    Repeat = "YES",
                    UserId = userId,
                    InstallmentAmount = editRecurrentDTO.newAmount.Value
                };

                await _cardTransactionRepository.AddAsync(newCardTransaction);

            }
            return Ok();

        }
    }     
}
