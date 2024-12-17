﻿using JazFinanzasApp.API.Interfaces;
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

        public ReportController(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAsset_UserRepository asset_UserRepository,
            ICardTransactionRepository cardTransactionRepository,
            IAssetQuoteRepository assetQuoteRepository
            )
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _asset_UserRepository = asset_UserRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _assetQuoteRepository = assetQuoteRepository;            
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


            // Get the balance for the user by account

            var balanceDTO = await _transactionRepository.GetTotalsBalanceByUserAsync(userId);
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

        [HttpGet("IncExpStatsDollar")]
        public async Task<IActionResult> GetIncExpDollarStats([FromQuery] DateTime month)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var incExpStatsDTO = await _transactionRepository.GetDollarIncExpStatsAsync(userId, month);
            return Ok(incExpStatsDTO);
        }

        [HttpGet("IncExpStatsPesos")]
        public async Task<IActionResult> GetIncExpPesosStats([FromQuery] DateTime month)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim.Value);

            var incExpStatsDTO = await _transactionRepository.GetPesosIncExpStatsAsync(userId, month);
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

            var stockStats = await _transactionRepository.GetStockStatsAsync(userId, id, "BOLSA");

            var stockStatsGral = await _transactionRepository.GetStocksGralStatsAsync(userId, "BOLSA");

            var stockStatsDTO = new StockStatsDTO
            {
                StockStatsInd = stockStats.ToArray(),
                StockStatsGral = stockStatsGral.ToArray()
            };

            return Ok(stockStatsDTO);
        }
       
            
    }
}