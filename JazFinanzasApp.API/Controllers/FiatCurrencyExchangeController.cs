﻿using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO;
using JazFinanzasApp.API.Models.DTO.InvestmentTransaction;
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
        private readonly ITransactionRepository _transactionRepository;
        private readonly IInvestmentTransactionRepository _investmentTransactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;

        public FiatCurrencyExchangeController(ITransactionRepository transactionRepository, IInvestmentTransactionRepository investmentTransactionRepository, IAssetRepository assetRepository, IAccountRepository accountRepository, IAssetQuoteRepository assetQuoteRepository)
        {
            _transactionRepository = transactionRepository;
            _investmentTransactionRepository = investmentTransactionRepository;
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
                var (transactions, totalCount) = await _investmentTransactionRepository.GetPaginatedInvestmentTransactions(userId, page, pageSize, environment);

                var transactionsDTO = transactions.Select(m => new CurrencyExchangeListDTO
                {
                    Id = m.Id,
                    Date = m.Date,
                    ExpenseAsset = m.ExpenseTransaction?.Asset?.Name,
                    ExpenseAccount = m.ExpenseTransaction?.Account?.Name,
                    ExpenseAmount = m.ExpenseTransaction?.Amount,
                    IncomeAsset = m.IncomeTransaction?.Asset?.Name,
                    IncomeAccount = m.IncomeTransaction?.Account?.Name,
                    IncomeAmount = m.IncomeTransaction?.Amount
                });

                return Ok(new { transactionsDTO, totalCount });
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


                //create transactions
                var expenseTransaction = new Transaction
                {
                    AccountId = exchangeTransactionDTO.ExpenseAccountId.Value,
                    Account = expenseAccount,
                    AssetId = exchangeTransactionDTO.ExpenseAssetId.Value,
                    Asset = expenseAsset,
                    Date = exchangeTransactionDTO.Date,
                    MovementType = "E",
                    TransactionClassId = null,
                    Detail = "Currency Exchange",
                    Amount = -exchangeTransactionDTO.ExpenseAmount.Value,
                    QuotePrice = expenseQuote.Value,
                    UserId = userId
                };

                var incomeTransaction = new Transaction
                {
                    AccountId = exchangeTransactionDTO.IncomeAccountId.Value,
                    Account = incomeAccount,
                    AssetId = exchangeTransactionDTO.IncomeAssetId.Value,
                    Asset = incomeAsset,
                    Date = exchangeTransactionDTO.Date,
                    MovementType = "I",
                    TransactionClassId = null,
                    Detail = "Currency Exchange",
                    Amount = exchangeTransactionDTO.IncomeAmount.Value,
                    QuotePrice = incomeQuote.Value,
                    UserId = userId
                };

                //save transactions
                expenseTransaction = await _transactionRepository.AddAsyncReturnObject(expenseTransaction);
                expenseId = expenseTransaction.Id;

                incomeTransaction = await _transactionRepository.AddAsyncReturnObject(incomeTransaction);
                incomeId = incomeTransaction.Id;


                //create and save investment transaction
                var investmentTransaction = new InvestmentTransaction
                {
                    Date = exchangeTransactionDTO.Date,
                    Environment = "CurrencyExchange",
                    MovementType = "EX",  
                    CommerceType = "CurrencyExchange",
                    ExpenseTransactionId = expenseId,
                    IncomeTransactionId = incomeId,
                    UserId = userId
                };

                await _investmentTransactionRepository.AddAsync(investmentTransaction);
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
                var investmentTransaction = await _investmentTransactionRepository.GetInvestmentTransactionById(id);

                if (investmentTransaction == null)
                {
                    return NotFound();
                }

                if (investmentTransaction.UserId != userId)
                {
                    return Unauthorized();
                }


                try
                {
                    await _investmentTransactionRepository.BeginTransactionAsync();

                    if (investmentTransaction.IncomeTransaction != null)
                    {
                        await _transactionRepository.DeleteAsync(investmentTransaction.IncomeTransactionId.Value);
                    }

                    if (investmentTransaction.ExpenseTransaction != null)
                    {
                        await _transactionRepository.DeleteAsync(investmentTransaction.ExpenseTransactionId.Value);
                    }

                    await _investmentTransactionRepository.DeleteAsync(investmentTransaction.Id);
                    await _investmentTransactionRepository.CommitTransactionAsync();
                    return Ok();
                }
                catch
                {
                    await _investmentTransactionRepository.RollbackTransactionAsync();
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
