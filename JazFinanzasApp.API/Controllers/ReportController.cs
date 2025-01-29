using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.CardTransaction;
using JazFinanzasApp.API.Models.DTO.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAsset_UserRepository _asset_UserRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IAssetTypeRepository _assetTypeRepository;

        public ReportController(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAsset_UserRepository asset_UserRepository,
            ICardTransactionRepository cardTransactionRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IAssetTypeRepository assetTypeRepository
            )
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _asset_UserRepository = asset_UserRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _assetQuoteRepository = assetQuoteRepository;   
            _assetTypeRepository = assetTypeRepository;
        }

        [HttpGet("Balance")]
        public async Task<IActionResult> GetTotalsBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            // get the reference assets for the user
            var referenceAssets = await _asset_UserRepository.GetReferenceAssetsAsync(userId);


            // Get the balance for the user by asset        
            //create the ienumerable of balancedto
            var balanceDTO = new List<TotalsBalanceDTO>();

            if (referenceAssets.Count() == 0)
            {
                var asset = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                var balance = await _transactionRepository.GetTotalsBalanceByUserAsync(userId, asset);
                balanceDTO.Add(balance);
                return Ok(balanceDTO);
            }

            

            foreach (var asset in referenceAssets)
            {
                var balance = await _transactionRepository.GetTotalsBalanceByUserAsync(userId, asset.Asset);
                balanceDTO.Add(balance);
            }

            return Ok(balanceDTO);
        }        

        [HttpGet("Balance/{id}")]
        public async Task<IActionResult> GetBalance(int id)
        {
            

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var asset = await _assetRepository.GetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }


            var balanceDTO = await _transactionRepository.GetBalanceByAssetAndUserAsync(id, userId);
            // Get the balance for the user and assetId by account


            return Ok(balanceDTO);
        }

        [HttpGet("IncExpStats")]
        public async Task<IActionResult> GetIncExpStats([FromQuery] DateTime month, [FromQuery] int assetId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var asset = await _assetRepository.GetByIdAsync(assetId);

            if (asset == null)
            {
                return NotFound();
            }

            if (asset.AssetTypeId != 1)
            {
                return BadRequest("El activo no es una moneda");
            }

            
            var incExpStatsDTO = await _transactionRepository.GetIncExpStatsAsync(userId, month, asset);
            return Ok(incExpStatsDTO);
            


        }

        [HttpGet("CardStats/{id}")]
        public async Task<IActionResult> GetCardStats(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            if (id != 0)
            {
                var card = await _assetRepository.GetByIdAsync(id);
                if (card == null)
                {
                    return NotFound();
                }
            } 

            IEnumerable<CardGraphDTO> PesosExpenses = await _cardTransactionRepository.GetCardStats(id, "Peso Argentino", userId);
            IEnumerable<CardGraphDTO> DollarExpenses = await _cardTransactionRepository.GetCardStats(id, "Dolar Estadounidense", userId);
           

            var today = DateTime.Now;
            

            var peso = await _assetRepository.GetAssetByNameAsync("Peso Argentino");
            var exchangeRate = await _assetQuoteRepository.GetQuotePrice(peso.Id, today, "TARJETA");

            var cardTransactions = await _cardTransactionRepository.GetCardTransactionsToPay(id, today, userId);

            var cardPayments = cardTransactions.Select(m =>
            {
                string installmentDisplay;
                if (m.Repeat == "YES")
                {
                    installmentDisplay = "Recurrente";
                }
                else
                {
                    // Calculamos la cuota actual con la diferencia en meses entre paymentMonth y FirstInstallment
                    var currentInstallment = ((today.Year - m.FirstInstallment.Year) * 12) + today.Month - m.FirstInstallment.Month + 1;
                    installmentDisplay = $"{currentInstallment}/{m.Installments}";
                }
                // Calculamos el valor en pesos si el movimiento está en dólares
                var valueInPesos = m.Asset.Name == "Dolar Estadounidense" ? m.InstallmentAmount * exchangeRate : m.InstallmentAmount;

                return new CardTransactionPaymentListDTO
                {
                    Date = m.Date,
                    Card = m.Card.Name,
                    //TransactionClassId = m.TransactionClassId,
                    TransactionClass = m.TransactionClass.Description,
                    Detail = m.Detail,
                    //AssetId = m.AssetId,
                    Asset = m.Asset.Name,
                    Installment = installmentDisplay,
                    InstallmentAmount = m.InstallmentAmount,
                    ValueInPesos = valueInPesos
                };


            }).ToList();

            var cardsStatsDTO = new CardsStatsDTO
            {
                PesosCardGraphDTO = PesosExpenses.ToArray(),
                DollarsCardGraphDTO = DollarExpenses.ToArray(),
                cardTransactionsDTO = cardPayments.ToArray()
            };

            return Ok(cardsStatsDTO);
        }    
        
        [HttpGet("StockStats/{id}")]
        public async Task<IActionResult> GetStockStats(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var assetType = await _assetRepository.GetByIdAsync(id);
            if (assetType == null)
            {
                return NotFound();
            }

            // get main reference asset
            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            int mainReferenceAssetId = mainReferenceAsset.AssetId;

            if (mainReferenceAsset == null)
            {
                // get dolar estadouniendese as main reference asset
                var dollarMainReferenceAsset = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                mainReferenceAssetId = dollarMainReferenceAsset.Id;
            }; 




            var stockStats = await _transactionRepository.GetStockStatsAsync(userId, id, "BOLSA", false, mainReferenceAssetId);

            var stockStatsGral = await _transactionRepository.GetStocksGralStatsAsync(userId, "BOLSA", mainReferenceAssetId);

            var stockStatsDTO = new StockStatsDTO
            {
                StockStatsInd = stockStats.ToArray(),
                StockStatsGral = stockStatsGral.ToArray()
            };

            return Ok(stockStatsDTO);
        }

        [HttpGet("CryptoGralStats")]
        public async Task<IActionResult> GetCryptoGralStats([FromQuery] bool includeStables)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            AssetType cryptoAsset = await _assetTypeRepository.GetByName("Criptomoneda");

            // get main reference asset
            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            int mainReferenceAssetId = mainReferenceAsset.AssetId;

            if (mainReferenceAsset == null)
            {
                // get dolar estadouniendese as main reference asset
                var dollarMainReferenceAsset = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                mainReferenceAssetId = dollarMainReferenceAsset.Id;
            };



            var CryptoGralStats = await _transactionRepository.GetStockStatsAsync(userId, cryptoAsset.Id, cryptoAsset.Environment, includeStables, mainReferenceAssetId);
            var CryptoStatsByDate = await _transactionRepository.GetCryptoStatsByDateAsync(userId, cryptoAsset.Id, cryptoAsset.Environment, 0, includeStables, mainReferenceAssetId);    
            var CryptoPurchasesStatsByMonth = await _transactionRepository.GetInvestmentsHoldingsStats(userId, cryptoAsset.Id, cryptoAsset.Environment, 0, includeStables, 12, mainReferenceAssetId);


            var CryptoGralStatsDTO = new CryptoGralStatsDTO
            {
                CryptoGralStats = CryptoGralStats.ToArray(),
                CryptoStatsByDate = CryptoStatsByDate.ToArray(),
                CryptoPurchasesStatsByMonth = CryptoPurchasesStatsByMonth.ToArray()
            };
            
            return Ok(CryptoGralStatsDTO);

        }

        [HttpGet("CryptoStats/{id}")]
        public async Task<IActionResult> GetCryptoStats(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var asset = await _assetRepository.GetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            // get main reference asset
            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            int mainReferenceAssetId = mainReferenceAsset.AssetId;

            if (mainReferenceAsset == null)
            {
                // get dolar estadouniendese as main reference asset
                var dollarMainReferenceAsset = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                mainReferenceAssetId = dollarMainReferenceAsset.Id;
            };


            var cryptoEvolution = await _assetQuoteRepository.GetAssetEvolutionStats(id, 6, mainReferenceAssetId);

            var balance = await _transactionRepository.GetBalanceByAssetAndUserAsync(id, userId);
            var cryptoTransactionsStats = await _transactionRepository.GetInvestmentsTransactionsStats(userId, id, mainReferenceAssetId);
            //var cryptoStatsGral = await _transactionRepository.GetStocksGralStatsAsync(userId, "CRIPTO", mainReferenceAssetId);
            var CryptoStatsEvolution = await _transactionRepository.GetCryptoStatsByDateAsync(userId, asset.AssetTypeId, "CRYPTO", id, true, mainReferenceAssetId); 
            var averageBuyValue = await _transactionRepository.GetAverageBuyValue(userId, id, mainReferenceAssetId);

            var cryptoRangeStats = new InvestmentRangeValuesStatsDTO
            {
                // min value that is not zero
                MinValue = CryptoStatsEvolution.Where(m => m.Value > 0).Min(m => m.Value),
                MaxValue = CryptoStatsEvolution.Max(m => m.Value),
                CurrentValue = CryptoStatsEvolution.Last().Value,
                AverageBuyValue = averageBuyValue
            };

            var cryptoStatsDTO = new CryptoStatsDTO
            {
                CryptoEvolutionStats = cryptoEvolution.ToArray(),
                CryptoBalanceStats = balance.ToArray(),
                CryptoTransactionsStats = cryptoTransactionsStats.ToArray(),
                CryptoRangeValuesStats = cryptoRangeStats
            };

            return Ok(cryptoStatsDTO);
        }

        [HttpGet("HomeStats")]
        public async Task<IActionResult> GetHomeStats()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            AssetType cryptoAsset = await _assetTypeRepository.GetByName("Criptomoneda");

            // get main reference asset
            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            int mainReferenceAssetId = mainReferenceAsset.AssetId;

            if (mainReferenceAsset == null)
            {
                // get dolar estadouniendese as main reference asset
                var dollarMainReferenceAsset = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                mainReferenceAssetId = dollarMainReferenceAsset.Id;
            };


            var stockStatsGral = await _transactionRepository.GetStocksGralStatsAsync(userId, "BOLSA", mainReferenceAssetId);
            var cryptoStatsGral = await _transactionRepository.GetStockStatsAsync(userId, cryptoAsset.Id, "CRYPTO", true, mainReferenceAssetId);

            var homeStatsDTO = new HomeStatsDTO
            {
                StockStatsGral = stockStatsGral.ToArray(),
                CryptoStatsGral = cryptoStatsGral.ToArray()
            };

            return Ok(homeStatsDTO);
        }      
            
    }
}
