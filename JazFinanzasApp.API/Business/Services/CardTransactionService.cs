using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class CardTransactionService : ICardTransactionService
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
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISharedExpenseRepository _sharedExpenseRepository;

        public CardTransactionService(
            ICardTransactionRepository cardTransactionRepository,
            ICardRepository cardRepository,
            IAsset_UserRepository assetUserRepository,
            ITransactionClassRepository transactionClassRepository,
            IAssetRepository assetRepository,
            IAssetQuoteRepository assetQuoteRepository,
            ICardPaymentRepository cardPaymentRepository,
            IAccountRepository accountRepository,
            IAccount_AssetTypeRepository account_AssetTypeRepository,
            ITransactionRepository transactionRepository,
            IPortfolioRepository portfolioRepository,
            IUnitOfWork unitOfWork,
            ISharedExpenseRepository sharedExpenseRepository)
        {
            _cardTransactionRepository = cardTransactionRepository;
            _cardRepository = cardRepository;
            _assetUserRepository = assetUserRepository;
            _transactionClassRepository = transactionClassRepository;
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _cardPaymentRepository = cardPaymentRepository;
            _accountRepository = accountRepository;
            _account_AssetTypeRepository = account_AssetTypeRepository;
            _transactionRepository = transactionRepository;
            _portfolioRepository = portfolioRepository;
            _unitOfWork = unitOfWork;
            _sharedExpenseRepository = sharedExpenseRepository;
        }

        public async Task AddCardTransactionAsync(int userId, CardTransactionAddDTO dto)
        {
            var card = await _cardRepository.GetByIdAsync(dto.CardId)
                ?? throw new NotFoundException("Card not found");
            var asset = await _assetRepository.GetByIdAsync(dto.AssetId)
                ?? throw new NotFoundException("Asset not found");
            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, dto.AssetId)
                ?? throw new UnauthorizedDomainException();
            var transactionClass = await _transactionClassRepository.GetByIdAsync(dto.TransactionClassId)
                ?? throw new NotFoundException("Transaction class not found");

            dto.FirstInstallment = new DateTime(dto.FirstInstallment.Year, dto.FirstInstallment.Month, 1);
            dto.LastInstallment = new DateTime(dto.LastInstallment.Year, dto.LastInstallment.Month, 1);

            await _cardTransactionRepository.AddAsync(new CardTransaction
            {
                Date = dto.Date,
                Detail = dto.Detail,
                CardId = dto.CardId,
                Card = card,
                TransactionClassId = dto.TransactionClassId,
                TransactionClass = transactionClass,
                AssetId = dto.AssetId,
                Asset = asset,
                TotalAmount = dto.TotalAmount,
                Installments = dto.Installments,
                FirstInstallment = dto.FirstInstallment,
                LastInstallment = dto.LastInstallment,
                Repeat = dto.Repeat,
                UserId = userId,
                InstallmentAmount = dto.TotalAmount / dto.Installments
            });
        }

        public async Task<IEnumerable<CardTransactionsPendingDTO>> GetPendingCardTransactionsAsync(int userId)
        {
            var results = await _cardTransactionRepository.GetPendingCardTransactionsAsync(userId);
            return results.Select(r => new CardTransactionsPendingDTO
            {
                Id = r.Id,
                Date = r.Date,
                Card = r.Card,
                TransactionClass = r.TransactionClass,
                Detail = r.Detail,
                Installments = r.Installments,
                Asset = r.Asset,
                AssetSymbol = r.AssetSymbol,
                TotalAmount = r.TotalAmount,
                FirstInstallment = r.FirstInstallment,
                LastInstallment = r.LastInstallment,
                InstallmentAmount = r.InstallmentAmount
            });
        }

        public async Task<IEnumerable<CardTransactionPaymentListDTO>> GetCardPaymentsAsync(int userId, int cardId, DateTime paymentMonth)
        {
            var card = await _cardRepository.GetByIdAsync(cardId)
                ?? throw new NotFoundException("Card not found");

            var isPaymentMade = await _cardPaymentRepository.IsPaymentAlreadyMadeAsync(cardId, paymentMonth);
            if (isPaymentMade) throw new BusinessRuleException("Payment already made");

            var peso = await _assetRepository.GetAssetByNameAsync("Peso Argentino");
            var exchangeRate = await _assetQuoteRepository.GetQuotePrice(peso.Id, DateTime.Today, "TARJETA");

            var cardTransactions = await _cardTransactionRepository.GetCardTransactionsToPay(cardId, paymentMonth, userId);

            return cardTransactions.Select(m =>
            {
                var currentInstallment = ((paymentMonth.Year - m.FirstInstallment.Year) * 12) + paymentMonth.Month - m.FirstInstallment.Month + 1;
                var installmentDisplay = m.Repeat == "YES" ? "Recurrente" : $"{currentInstallment}/{m.Installments}";

                var valueInPesos = m.Asset.Name == "Dolar Estadounidense" ? m.InstallmentAmount * exchangeRate : m.InstallmentAmount;

                return new CardTransactionPaymentListDTO
                {
                    CardTransactionId = m.Id,
                    Date = m.Date,
                    TransactionClassId = m.TransactionClassId,
                    TransactionClass = m.TransactionClass.Description,
                    Detail = m.Detail,
                    AssetId = m.AssetId,
                    Asset = m.Asset.Name,
                    Installment = installmentDisplay,
                    InstallmentNumber = currentInstallment,
                    InstallmentAmount = m.InstallmentAmount,
                    ValueInPesos = valueInPesos
                };
            }).ToList();
        }

        public async Task RegisterCardPaymentAsync(int userId, CardTransactionPaymentDTO dto)
        {
            var card = await _cardRepository.GetByIdAsync(dto.CardId)
                ?? throw new NotFoundException("Card not found");

            var account = await _accountRepository.GetByIdAsync(dto.accountId)
                ?? throw new NotFoundException("Account not found");

            var account_AssetType = await _account_AssetTypeRepository
                .GetAccount_AssetTypeByAccountIdAndAssetTypeNameAsync(account.Id, "Moneda")
                ?? throw new NotFoundException("Account_AssetType not found");

            var peso = await _assetRepository.GetAssetByNameAsync("Peso Argentino");
            var dolar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
            var quotePrice = await _assetQuoteRepository.GetQuotePrice(peso.Id, dto.PaymentDate, "BLUE");

            var portfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                ?? throw new NotFoundException("Default portfolio not found");

            var balance = await _transactionRepository.GetBalance(account.Id, peso.Id, portfolio.Id);
            if (balance < dto.PesosAmount) throw new BusinessRuleException("Not enough balance in account");

            if (dto.DolarAmount != null)
            {
                var balanceDolar = await _transactionRepository.GetBalance(account.Id, dolar.Id, portfolio.Id);
                if (balanceDolar < dto.DolarAmount) throw new BusinessRuleException("Not enough balance in account");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var cardTransaction in dto.CardTransactions)
                {
                    var asset = await _assetRepository.GetByIdAsync(cardTransaction.AssetId);
                    if (asset == null || (asset.Name != "Peso Argentino" && asset.Name != "Dolar Estadounidense"))
                        throw new BusinessRuleException("Error in Validation");
                    cardTransaction.Asset = asset.Name;

                    var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardTransaction.AssetId)
                        ?? throw new BusinessRuleException("Error in Validation");

                    var transactionClass = await _transactionClassRepository.GetByIdAsync(cardTransaction.TransactionClassId)
                        ?? throw new BusinessRuleException("Error in Validation");
                    cardTransaction.TransactionClass = transactionClass.Description;

                    var transaction = BuildCardPaymentTransaction(dto, cardTransaction, userId, peso, dolar, quotePrice, portfolio.Id);
                    await ApplySharedExpenseReimbursementsAsync(cardTransaction.CardTransactionId, cardTransaction.InstallmentNumber, transaction);
                    await _transactionRepository.AddAsyncTransaction(transaction);
                }

                var cardExpensesClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Gastos Tarjeta", userId)
                    ?? throw new NotFoundException("Transaction class 'Gastos Tarjeta' not found");

                await _transactionRepository.AddAsyncTransaction(new Transaction
                {
                    Date = dto.PaymentDate,
                    Detail = $"Gastos Tarjeta - {card.Name}",
                    AccountId = dto.accountId,
                    PortfolioId = portfolio.Id,
                    TransactionClassId = cardExpensesClass.Id,
                    MovementType = "E",
                    UserId = userId,
                    Amount = -dto.CardExpenses,
                    AssetId = peso.Id,
                    QuotePrice = quotePrice
                });

                await _unitOfWork.CommitTransactionAsync();

                await _cardPaymentRepository.AddAsync(new CardPayment
                {
                    CardId = dto.CardId,
                    Card = card,
                    Date = dto.PaymentMonth
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<EditRecurrentListDTO> GetRecurrentTransactionAsync(int userId, int id)
        {
            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card transaction not found");
            if (cardTransaction.Repeat != "YES") throw new BusinessRuleException("Card transaction is not recurrent");
            if (cardTransaction.UserId != userId) throw new UnauthorizedDomainException();

            var card = await _cardRepository.GetByIdAsync(cardTransaction.CardId)
                ?? throw new NotFoundException("Card not found");
            var asset = await _assetRepository.GetByIdAsync(cardTransaction.AssetId)
                ?? throw new NotFoundException("Asset not found");
            var transactionClass = await _transactionClassRepository.GetByIdAsync(cardTransaction.TransactionClassId)
                ?? throw new NotFoundException("Transaction class not found");
            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, cardTransaction.AssetId)
                ?? throw new UnauthorizedDomainException();

            return new EditRecurrentListDTO
            {
                Id = cardTransaction.Id,
                Date = cardTransaction.Date,
                Card = card.Name,
                Description = cardTransaction.Detail,
                Amount = cardTransaction.InstallmentAmount,
                FirstInstallment = cardTransaction.FirstInstallment
            };
        }

        public async Task UpdateRecurrentTransactionAsync(int userId, int id, EditRecurrentDTO dto)
        {
            var newFirstInstallment = new DateTime(dto.newDate.Year, dto.newDate.Month, 1);
            var oldLastInstallment = newFirstInstallment.AddMonths(-1);

            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card transaction not found");
            if (cardTransaction.Repeat != "YES") throw new BusinessRuleException("Card transaction is not recurrent");
            if (cardTransaction.UserId != userId) throw new UnauthorizedDomainException();
            if (oldLastInstallment < cardTransaction.FirstInstallment)
                throw new BusinessRuleException("Date is lower than first installment");

            cardTransaction.Repeat = "CLOSED";
            cardTransaction.LastInstallment = oldLastInstallment;
            cardTransaction.UpdatedAt = DateTime.UtcNow;
            await _cardTransactionRepository.UpdateAsync(cardTransaction);

            if (dto.isUpdate)
            {
                await _cardTransactionRepository.AddAsync(new CardTransaction
                {
                    Date = dto.newDate,
                    Detail = cardTransaction.Detail,
                    CardId = cardTransaction.CardId,
                    Card = cardTransaction.Card,
                    TransactionClassId = cardTransaction.TransactionClassId,
                    TransactionClass = cardTransaction.TransactionClass,
                    AssetId = cardTransaction.AssetId,
                    Asset = cardTransaction.Asset,
                    TotalAmount = dto.newAmount.Value,
                    Installments = cardTransaction.Installments,
                    FirstInstallment = newFirstInstallment,
                    Repeat = "YES",
                    UserId = userId,
                    InstallmentAmount = dto.newAmount.Value
                });
            }
        }

        public async Task DeleteCardTransactionAsync(int userId, int id)
        {
            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card transaction not found");
            if (cardTransaction.UserId != userId) throw new UnauthorizedDomainException();
            await _cardTransactionRepository.DeleteAsync(id);
        }

        private async Task ApplySharedExpenseReimbursementsAsync(int cardTransactionId, int installmentNumber, Transaction expenseTransaction)
        {
            var sharedExpense = await _sharedExpenseRepository.GetByCardTransactionIdAsync(cardTransactionId);
            if (sharedExpense == null)
                return;

            foreach (var split in sharedExpense.Splits)
            {
                if (split.SplitType == SharedExpenseSplitType.BankPromotion)
                    await ApplyPromotionInstallmentAsync(split, installmentNumber, expenseTransaction);
                else
                    await ApplyPersonPoolInstallmentAsync(split, expenseTransaction);
            }
        }

        // Las promociones quedan pre-particionadas por cuota al registrarse (FIFO); solo se aplica
        // lo que haya quedado etiquetado exactamente para la cuota que se está pagando ahora.
        private async Task ApplyPromotionInstallmentAsync(SharedExpenseSplit split, int installmentNumber, Transaction expenseTransaction)
        {
            var reimbursements = await _sharedExpenseRepository.GetReimbursementsBySplitIdAsync(split.Id);
            var matching = reimbursements.Where(r => r.InstallmentNumber == installmentNumber).ToList();
            if (!matching.Any())
                return;

            var toApply = matching.Sum(r => r.Amount);
            expenseTransaction.Amount += toApply;
            split.AmountApplied += toApply;
            split.UpdatedAt = DateTime.UtcNow;

            foreach (var reimbursement in matching)
            {
                await _sharedExpenseRepository.DeleteReimbursementAsync(reimbursement.Id);
                await _transactionRepository.DeleteAsync(reimbursement.TransactionId);
            }

            await _sharedExpenseRepository.UpdateSplitAsync(split);
        }

        // Las personas pagan a un pool sin atar cada Transaction a una cuota específica (mes a mes o upfront);
        // cada cuota consume hasta InstallmentSplitAmount del pool disponible.
        private async Task ApplyPersonPoolInstallmentAsync(SharedExpenseSplit split, Transaction expenseTransaction)
        {
            var available = split.AmountReimbursed - split.AmountApplied;
            if (available <= 0)
                return;

            var toApply = Math.Min(available, split.InstallmentSplitAmount);
            if (toApply <= 0)
                return;

            expenseTransaction.Amount += toApply;
            split.AmountApplied += toApply;
            split.UpdatedAt = DateTime.UtcNow;

            await RemoveConsumedReimbursementsAsync(split);
            await _sharedExpenseRepository.UpdateSplitAsync(split);
        }

        private async Task RemoveConsumedReimbursementsAsync(SharedExpenseSplit split)
        {
            var reimbursements = await _sharedExpenseRepository.GetReimbursementsBySplitIdAsync(split.Id);

            decimal cumulative = 0;
            foreach (var reimbursement in reimbursements)
            {
                cumulative += reimbursement.Amount;
                if (split.AmountApplied < cumulative)
                    break;

                await _sharedExpenseRepository.DeleteReimbursementAsync(reimbursement.Id);
                await _transactionRepository.DeleteAsync(reimbursement.TransactionId);
            }
        }

        private Transaction BuildCardPaymentTransaction(
            CardTransactionPaymentDTO paymentDTO,
            CardTransactionPaymentListDTO cardTx,
            int userId,
            Asset peso,
            Asset dolar,
            decimal quotePrice,
            int portfolioId)
        {
            var transaction = new Transaction
            {
                Date = paymentDTO.PaymentMonth,
                Detail = $"(Tarjeta | {cardTx.Installment}) {cardTx.Detail}",
                AccountId = paymentDTO.accountId,
                PortfolioId = portfolioId,
                TransactionClassId = cardTx.TransactionClassId,
                MovementType = "E",
                UserId = userId,
                Amount = paymentDTO.PaymentAsset == "P+D"
                    ? -cardTx.InstallmentAmount
                    : -cardTx.ValueInPesos,
                AssetId = cardTx.Asset == "Dolar Estadounidense" && paymentDTO.PaymentAsset == "P+D"
                    ? dolar.Id
                    : peso.Id,
                QuotePrice = quotePrice
            };
            transaction.Asset = transaction.AssetId == dolar.Id ? dolar : peso;
            return transaction;
        }
    }
}
