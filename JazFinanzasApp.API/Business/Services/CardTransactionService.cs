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
        private readonly ICardTransactionDiscountRepository _cardTransactionDiscountRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ITripSuggestionDismissalRepository _tripSuggestionDismissalRepository;
        private readonly ISharedEventMovementRepository _sharedEventMovementRepository;
        private readonly ICardTransactionDiscountService _cardTransactionDiscountService;

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
            ISharedExpenseRepository sharedExpenseRepository,
            ICardTransactionDiscountRepository cardTransactionDiscountRepository,
            ITripRepository tripRepository,
            ITripSuggestionDismissalRepository tripSuggestionDismissalRepository,
            ISharedEventMovementRepository sharedEventMovementRepository,
            ICardTransactionDiscountService cardTransactionDiscountService)
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
            _cardTransactionDiscountRepository = cardTransactionDiscountRepository;
            _tripRepository = tripRepository;
            _tripSuggestionDismissalRepository = tripSuggestionDismissalRepository;
            _sharedEventMovementRepository = sharedEventMovementRepository;
            _cardTransactionDiscountService = cardTransactionDiscountService;
        }

        public async Task<int> AddCardTransactionAsync(int userId, CardTransactionAddDTO dto)
        {
            var card = await _cardRepository.GetByIdAsync(dto.CardId)
                ?? throw new NotFoundException("Card not found");
            var asset = await _assetRepository.GetByIdAsync(dto.AssetId)
                ?? throw new NotFoundException("Asset not found");
            var assetUser = await _assetUserRepository.GetUserAssetAsync(userId, dto.AssetId)
                ?? throw new UnauthorizedDomainException();
            var transactionClass = await _transactionClassRepository.GetByIdAsync(dto.TransactionClassId)
                ?? throw new NotFoundException("Transaction class not found");

            if (dto.TripId != null)
            {
                var trip = await _tripRepository.GetByIdAsync(dto.TripId.Value)
                    ?? throw new NotFoundException("Trip not found");
                if (trip.UserId != userId) throw new UnauthorizedDomainException();
            }

            dto.FirstInstallment = new DateTime(dto.FirstInstallment.Year, dto.FirstInstallment.Month, 1);
            dto.LastInstallment = new DateTime(dto.LastInstallment.Year, dto.LastInstallment.Month, 1);

            var cardTransaction = await _cardTransactionRepository.AddAsyncReturnObject(new CardTransaction
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
                InstallmentAmount = dto.TotalAmount / dto.Installments,
                TripId = dto.TripId
            });

            return cardTransaction.Id;
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
                InstallmentAmount = r.InstallmentAmount,
                TripId = r.TripId,
                TripName = r.TripName
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

            if (dto.NextClosingDate.HasValue && dto.NextDueDate.HasValue
                && dto.NextDueDate.Value < dto.NextClosingDate.Value)
            {
                throw new BusinessRuleException("NextDueDate must be on or after NextClosingDate");
            }

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
                // Primero se materializa el saldo a favor que el banco aplicó en este resumen, y recién
                // después corre el loop de cuotas. Al revés, el crédito de este mes no llegaría a tiempo
                // para la cuota de este mes (D7).
                await ApplyCardCreditAsync(dto, userId);

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

                    // Las filas agregadas a mano en el formulario de pago llegan con Id 0: son gastos que
                    // nunca se cargaron como consumo. Se les crea el CardTransaction para que el gasto exista
                    // como tal y se pueda etiquetar a un viaje o ver en las estadísticas de tarjeta (D3bis:
                    // el gasto de tarjeta se cuenta siempre desde el consumo). Antes solo quedaba la cuota,
                    // huérfana y sin forma de rastrearla.
                    CardTransaction? consumoCreado = null;
                    if (cardTransaction.CardTransactionId == 0)
                    {
                        consumoCreado = BuildManualCardTransaction(dto, cardTransaction, userId, asset, transactionClass, card);
                        await _cardTransactionRepository.AddAsyncTransaction(consumoCreado);
                    }

                    var transaction = BuildCardPaymentTransaction(dto, cardTransaction, userId, peso, dolar, quotePrice, portfolio.Id);
                    // Se vincula por la propiedad de navegación y no por el Id: el consumo todavía no se
                    // persistió, y EF resuelve la FK al guardar toda la transacción de una.
                    if (consumoCreado != null) transaction.CardTransaction = consumoCreado;
                    await ApplySharedExpenseReimbursementsAsync(cardTransaction.CardTransactionId, cardTransaction.InstallmentNumber, transaction);
                    await ApplyCardTransactionDiscountInstallmentAsync(cardTransaction.CardTransactionId, cardTransaction.InstallmentNumber, transaction, dto.accountId, dto.PaymentDate, userId);
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

                if (dto.NextClosingDate.HasValue || dto.NextDueDate.HasValue)
                {
                    if (dto.NextClosingDate.HasValue) card.NextClosingDate = dto.NextClosingDate.Value;
                    if (dto.NextDueDate.HasValue) card.NextDueDate = dto.NextDueDate.Value;
                    card.UpdatedAt = DateTime.UtcNow;
                    await _cardRepository.UpdateAsync(card);
                }
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

            if (await _sharedEventMovementRepository.IsCardTransactionReferencedAsync(id))
                throw new BusinessRuleException("Este gasto de tarjeta pertenece a un evento compartido; se elimina desde el evento");

            await _tripSuggestionDismissalRepository.DeleteByCardTransactionIdAsync(id);
            await _cardTransactionRepository.DeleteAsync(id);
        }

        private async Task ApplySharedExpenseReimbursementsAsync(int cardTransactionId, int installmentNumber, Transaction expenseTransaction)
        {
            var sharedExpense = await _sharedExpenseRepository.GetByCardTransactionIdAsync(cardTransactionId);
            if (sharedExpense == null)
                return;

            foreach (var split in sharedExpense.Splits)
            {
                await ApplyPersonPoolInstallmentAsync(split, expenseTransaction);
            }
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

        // El banco aplica el saldo a favor de la tarjeta contra el total del resumen. Acá se reparte
        // ese monto entre los descuentos que todavía tienen crédito pendiente, del más viejo al más
        // nuevo, y cada tramo se materializa como ingreso en la cuenta con la que se está pagando.
        // A partir de ahí el consumo contra las cuotas es el de siempre.
        private async Task ApplyCardCreditAsync(CardTransactionPaymentDTO dto, int userId)
        {
            if (dto.CardCreditApplied <= 0)
                return;

            var pendientes = (await _cardTransactionDiscountRepository.GetPendingOnCardAsync(dto.CardId, userId)).ToList();
            var totalPendiente = pendientes.Sum(d => d.Amount - d.AmountMaterialized);

            if (dto.CardCreditApplied > totalPendiente)
                throw new BusinessRuleException("El saldo a favor aplicado supera el pendiente en la tarjeta");

            decimal restante = dto.CardCreditApplied;
            foreach (var discount in pendientes)
            {
                if (restante <= 0)
                    break;

                var disponible = discount.Amount - discount.AmountMaterialized;
                var tramo = Math.Min(restante, disponible);
                if (tramo <= 0)
                    continue;

                await _cardTransactionDiscountService.MaterializeAsync(discount, tramo, dto.accountId, dto.PaymentDate, userId);
                restante -= tramo;
            }
        }

        // El descuento queda pre-particionado por cuota exacta al crearse (FIFO); solo se aplica
        // lo que haya quedado etiquetado exactamente para la cuota que se está pagando ahora.
        private async Task ApplyCardTransactionDiscountInstallmentAsync(int cardTransactionId, int installmentNumber, Transaction expenseTransaction, int payingAccountId, DateTime paymentDate, int userId)
        {
            var discount = await _cardTransactionDiscountRepository.GetByCardTransactionIdAsync(cardTransactionId);
            if (discount == null)
                return;

            var installments = await _cardTransactionDiscountRepository.GetInstallmentsByDiscountIdAsync(discount.Id);
            var matching = installments.Where(i => i.InstallmentNumber == installmentNumber).ToList();
            if (!matching.Any())
                return;

            var toApply = matching.Sum(i => i.Amount);
            expenseTransaction.Amount += toApply;
            discount.AmountApplied += toApply;
            discount.UpdatedAt = DateTime.UtcNow;

            foreach (var installment in matching)
            {
                // Si el ingreso del reintegro vive en otra cuenta (porque se rescató ahí), el canje
                // descuadraría las dos: la que paga quedaría inflada y la del reintegro corta.
                var income = await _transactionRepository.GetByIdAsync(installment.TransactionId);
                if (income != null && income.AccountId != payingAccountId)
                    await EmitCrossAccountTransferAsync(income, payingAccountId, installment.Amount, paymentDate, userId);

                await _cardTransactionDiscountRepository.DeleteInstallmentAsync(installment.Id);
                await _transactionRepository.DeleteAsync(installment.TransactionId);
            }

            await _cardTransactionDiscountRepository.UpdateAsync(discount);
        }

        // Abaratar una cuota es un canje: desaparece el ingreso del reintegro y, por ese mismo importe,
        // se achica el egreso de la cuota. Para que el saldo no se mueva, las dos puntas tienen que
        // estar en la misma cuenta. Cuando el reintegro se rescató a otra, se agrega un pase entre
        // cuentas propias que cierra el círculo: la que paga entrega el importe y la del reintegro lo
        // recibe. No cuenta como gasto ni como ingreso (MovementType EX, sin clase), así que no
        // ensucia ninguna categoría. Mismo patrón que TransactionService.RefundTransactionAsync.
        private async Task EmitCrossAccountTransferAsync(Transaction income, int payingAccountId, decimal amount, DateTime date, int userId)
        {
            var time = DateTime.UtcNow;
            const string prefijo = "Descuento - ";
            var compra = income.Detail != null && income.Detail.StartsWith(prefijo)
                ? income.Detail.Substring(prefijo.Length)
                : income.Detail;
            var detalle = "Pase por descuento - " + compra;

            await _transactionRepository.AddAsyncTransaction(new Transaction
            {
                AccountId = payingAccountId,
                PortfolioId = income.PortfolioId,
                AssetId = income.AssetId,
                Date = date,
                MovementType = "EX",
                TransactionClassId = null,
                Detail = detalle,
                Amount = -amount,
                UserId = userId,
                QuotePrice = income.QuotePrice,
                CreatedAt = time,
                UpdatedAt = time
            });

            await _transactionRepository.AddAsyncTransaction(new Transaction
            {
                AccountId = income.AccountId,
                PortfolioId = income.PortfolioId,
                AssetId = income.AssetId,
                Date = date,
                MovementType = "EX",
                TransactionClassId = null,
                Detail = detalle,
                Amount = amount,
                UserId = userId,
                QuotePrice = income.QuotePrice,
                CreatedAt = time,
                UpdatedAt = time
            });
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

                // Si la devolución se saldó a través de un Evento Compartido, su Transaction es el placeholder
                // que creó el motor de pagos y sigue referenciada desde SharedEventPaymentAllocations (FK real).
                // El reintegro se consolida igual dentro de la cuota — se suelta la FK y se deja marcado que el
                // placeholder ya no existe, así el pago del evento pide un ajuste en vez de revertirse solo.
                await _transactionRepository.DetachConsumedIncomeFromSharedEventPaymentAllocationsAsync(reimbursement.TransactionId);
                await _transactionRepository.DeleteAsync(reimbursement.TransactionId);
            }
        }

        // Consumo que respalda una fila agregada a mano al pagar la tarjeta. Es de una sola cuota y se
        // devenga en el mes que se está pagando, así que no vuelve a aparecer en la lista del mes siguiente.
        private static CardTransaction BuildManualCardTransaction(
            CardTransactionPaymentDTO paymentDTO,
            CardTransactionPaymentListDTO cardTx,
            int userId,
            Asset asset,
            TransactionClass transactionClass,
            Card card)
        {
            var mes = new DateTime(paymentDTO.PaymentMonth.Year, paymentDTO.PaymentMonth.Month, 1);

            return new CardTransaction
            {
                // El formulario inicializa la fecha vacía; si no la completaron, vale la del resumen.
                Date = cardTx.Date == default ? mes : cardTx.Date,
                Detail = cardTx.Detail,
                CardId = paymentDTO.CardId,
                Card = card,
                TransactionClassId = cardTx.TransactionClassId,
                TransactionClass = transactionClass,
                AssetId = cardTx.AssetId,
                Asset = asset,
                TotalAmount = cardTx.InstallmentAmount,
                Installments = 1,
                FirstInstallment = mes,
                LastInstallment = mes,
                Repeat = "NO",
                InstallmentAmount = cardTx.InstallmentAmount,
                UserId = userId
            };
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
                // Las filas manuales del formulario de pago no tienen un CardTransaction real (llegan con Id 0);
                // la FK a CardTransactions no admite ese valor, así que se guarda null.
                CardTransactionId = cardTx.CardTransactionId == 0 ? null : cardTx.CardTransactionId,
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
