using JazFinanzasApp.API.Business.DTO.CardTransactionDiscount;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class CardTransactionDiscountService : ICardTransactionDiscountService
    {
        private readonly ICardTransactionDiscountRepository _cardTransactionDiscountRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly ICardPaymentRepository _cardPaymentRepository;
        private readonly IQuotePriceResolver _quotePriceResolver;

        public CardTransactionDiscountService(
            ICardTransactionDiscountRepository cardTransactionDiscountRepository,
            ICardTransactionRepository cardTransactionRepository,
            IAccountRepository accountRepository,
            ITransactionClassRepository transactionClassRepository,
            ITransactionRepository transactionRepository,
            IPortfolioRepository portfolioRepository,
            ICardPaymentRepository cardPaymentRepository,
            IQuotePriceResolver quotePriceResolver)
        {
            _cardTransactionDiscountRepository = cardTransactionDiscountRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _accountRepository = accountRepository;
            _transactionClassRepository = transactionClassRepository;
            _transactionRepository = transactionRepository;
            _portfolioRepository = portfolioRepository;
            _cardPaymentRepository = cardPaymentRepository;
            _quotePriceResolver = quotePriceResolver;
        }

        public async Task<CardTransactionDiscountDetailDTO> CreateAsync(int userId, CardTransactionDiscountAddDTO dto)
        {
            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(dto.CardTransactionId)
                ?? throw new NotFoundException("Gasto de tarjeta no encontrado");
            if (cardTransaction.UserId != userId)
                throw new UnauthorizedDomainException();

            var existing = await _cardTransactionDiscountRepository.GetByCardTransactionIdAsync(dto.CardTransactionId);
            if (existing != null)
                throw new BusinessRuleException("Este gasto de tarjeta ya tiene un descuento asociado");

            if (dto.Amount > cardTransaction.TotalAmount)
                throw new BusinessRuleException("El monto del descuento no puede superar el monto del gasto de tarjeta");

            // Sin modalidad explícita se asume la de siempre, así un cliente que todavía no manda
            // el campo (el frontend hasta la Fase 6) sigue funcionando sin cambios.
            var creditTarget = string.IsNullOrWhiteSpace(dto.CreditTarget)
                ? CardTransactionDiscountCreditTarget.Account
                : dto.CreditTarget;

            if (!CardTransactionDiscountCreditTarget.IsValid(creditTarget))
                throw new BusinessRuleException("Modalidad de acreditación inválida");

            var acreditaEnCuenta = creditTarget == CardTransactionDiscountCreditTarget.Account;

            if (acreditaEnCuenta)
            {
                if (dto.AccountId is null)
                    throw new BusinessRuleException("Falta la cuenta donde el banco acreditó el reintegro");

                var account = await _accountRepository.GetByIdAsync(dto.AccountId.Value)
                    ?? throw new NotFoundException("Cuenta no encontrada");
                if (account.UserId != userId)
                    throw new UnauthorizedDomainException();
            }

            var discount = await _cardTransactionDiscountRepository.AddAsyncReturnObject(new CardTransactionDiscount
            {
                CardTransactionId = dto.CardTransactionId,
                Amount = dto.Amount,
                AmountApplied = 0,
                AmountMaterialized = 0,
                CreditTarget = creditTarget,
                CreditDate = dto.Date,
                Notes = dto.Notes,
                UserId = userId
            });

            // El reintegro acreditado en cuenta es una materialización del 100% en el momento del alta:
            // la plata ya está en la cuenta, así que se reparte entera entre las cuotas desde el vamos.
            // El acreditado sobre la tarjeta no genera ningún movimiento todavía: la plata está en la
            // tarjeta, no en una cuenta, y se materializa cuando el resumen la absorbe o cuando se rescata.
            if (acreditaEnCuenta)
                await MaterializeAsync(discount, dto.Amount, dto.AccountId!.Value, dto.Date, userId);

            return await MapToDetailDTOAsync(discount);
        }

        // Materializar = convertir parte de un descuento en plata dentro de una cuenta, repartiéndola
        // entre las cuotas que todavía no se pagaron. Es la única puerta por la que un descuento genera
        // Transactions, y la usan los tres caminos: alta en modalidad cuenta, resumen que absorbe el
        // saldo a favor de la tarjeta, y rescate a una cuenta.
        public async Task MaterializeAsync(CardTransactionDiscount discount, decimal amount, int accountId, DateTime date, int userId)
        {
            if (amount <= 0)
                throw new BusinessRuleException("El monto a acreditar debe ser mayor a cero");

            var pending = discount.Amount - discount.AmountMaterialized;
            if (amount > pending)
                throw new BusinessRuleException("El monto supera lo que resta acreditar del descuento");

            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(discount.CardTransactionId)
                ?? throw new NotFoundException("Gasto de tarjeta no encontrado");

            var account = await _accountRepository.GetByIdAsync(accountId)
                ?? throw new NotFoundException("Cuenta no encontrada");
            if (account.UserId != userId)
                throw new UnauthorizedDomainException();

            var transactionClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Reintegro", userId)
                ?? throw new NotFoundException("Clase de transacción 'Reintegro' no encontrada");

            var defaultPortfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                ?? throw new NotFoundException("Portfolio por defecto no encontrado");

            var plan = await BuildDistributionPlanAsync(discount, cardTransaction, amount);

            var quotePrice = await _quotePriceResolver.ResolveAsync(cardTransaction.AssetId, date);
            var detail = "Descuento - " + cardTransaction.Detail;

            foreach (var step in plan)
            {
                var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = account.Id,
                    Account = account,
                    PortfolioId = defaultPortfolio.Id,
                    Portfolio = defaultPortfolio,
                    AssetId = cardTransaction.AssetId,
                    Date = date,
                    MovementType = "I",
                    TransactionClassId = transactionClass.Id,
                    TransactionClass = transactionClass,
                    Detail = detail,
                    Amount = step.Portion,
                    UserId = userId,
                    QuotePrice = quotePrice
                });

                await _cardTransactionDiscountRepository.AddInstallmentAsync(new CardTransactionDiscountInstallment
                {
                    CardTransactionDiscountId = discount.Id,
                    TransactionId = incomeTransaction.Id,
                    Amount = step.Portion,
                    InstallmentNumber = step.InstallmentNumber,
                    Date = date
                });
            }

            discount.AmountMaterialized += amount;
            discount.UpdatedAt = DateTime.UtcNow;
            await _cardTransactionDiscountRepository.UpdateAsync(discount);
        }

        // FIFO sobre las cuotas todavía no pagadas: cada una absorbe todo lo que puede, topada a lo que
        // cuesta esa cuota menos lo que ya se le etiquetó en materializaciones anteriores. El plan se
        // arma entero y se valida antes de escribir nada: si el monto no entra en ninguna cuota, no
        // queremos haber dejado la mitad de las Transactions creadas.
        private async Task<List<DistributionStep>> BuildDistributionPlanAsync(
            CardTransactionDiscount discount, CardTransaction cardTransaction, decimal amount)
        {
            var existing = await _cardTransactionDiscountRepository.GetInstallmentsByDiscountIdAsync(discount.Id);
            var assignedByInstallment = existing
                .GroupBy(i => i.InstallmentNumber)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

            var paidMonths = (await _cardPaymentRepository.GetPaidMonthsAsync(cardTransaction.CardId))
                .Select(d => (d.Year, d.Month))
                .ToHashSet();

            var firstInstallmentMonth = new DateTime(cardTransaction.FirstInstallment.Year, cardTransaction.FirstInstallment.Month, 1);

            var plan = new List<DistributionStep>();
            decimal remaining = amount;

            for (int n = 1; n <= cardTransaction.Installments && remaining > 0; n++)
            {
                var month = firstInstallmentMonth.AddMonths(n - 1);
                if (paidMonths.Contains((month.Year, month.Month)))
                    continue;

                assignedByInstallment.TryGetValue(n, out var alreadyAssigned);
                var capacity = cardTransaction.InstallmentAmount - alreadyAssigned;
                if (capacity <= 0)
                    continue;

                var portion = Math.Min(remaining, capacity);
                plan.Add(new DistributionStep(n, portion));
                remaining -= portion;
            }

            if (remaining > 0)
                throw new BusinessRuleException("No quedan cuotas sin pagar donde aplicar el descuento");

            return plan;
        }

        private record DistributionStep(int InstallmentNumber, decimal Portion);

        public async Task<CardTransactionDiscountDetailDTO> GetByCardTransactionIdAsync(int userId, int cardTransactionId)
        {
            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(cardTransactionId)
                ?? throw new NotFoundException("Gasto de tarjeta no encontrado");
            if (cardTransaction.UserId != userId)
                throw new UnauthorizedDomainException();

            var discount = await _cardTransactionDiscountRepository.GetByCardTransactionIdAsync(cardTransactionId)
                ?? throw new NotFoundException("Este gasto de tarjeta no tiene un descuento asociado");

            return await MapToDetailDTOAsync(discount);
        }

        public async Task<IEnumerable<CardTransactionDiscountDetailDTO>> GetActiveByUserIdAsync(int userId)
        {
            var discounts = await _cardTransactionDiscountRepository.GetActiveByUserIdAsync(userId);
            var result = new List<CardTransactionDiscountDetailDTO>();
            foreach (var discount in discounts)
                result.Add(await MapToDetailDTOAsync(discount));

            return result;
        }

        public async Task DeleteAsync(int userId, int id)
        {
            var discount = await _cardTransactionDiscountRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Descuento no encontrado");
            if (discount.UserId != userId)
                throw new UnauthorizedDomainException();

            if (discount.AmountApplied > 0)
                throw new BusinessRuleException("No se puede eliminar un descuento que ya tiene cuotas aplicadas");

            var installments = await _cardTransactionDiscountRepository.GetInstallmentsByDiscountIdAsync(id);
            foreach (var installment in installments)
            {
                await _cardTransactionDiscountRepository.DeleteInstallmentAsync(installment.Id);
                await _transactionRepository.DeleteAsync(installment.TransactionId);
            }

            await _cardTransactionDiscountRepository.DeleteAsync(id);
        }

        // Rescate: el banco pasa el saldo a favor de la tarjeta a una cuenta, total o parcialmente.
        // A partir de acá ese tramo se comporta igual que un reintegro acreditado en cuenta.
        public async Task<CardTransactionDiscountDetailDTO> RescueAsync(int userId, int id, CardTransactionDiscountRescueDTO dto)
        {
            var discount = await _cardTransactionDiscountRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Descuento no encontrado");
            if (discount.UserId != userId)
                throw new UnauthorizedDomainException();

            if (discount.CreditTarget != CardTransactionDiscountCreditTarget.Card)
                throw new BusinessRuleException("Este descuento no tiene saldo a favor en la tarjeta");

            await MaterializeAsync(discount, dto.Amount, dto.AccountId, dto.Date, userId);

            return await MapToDetailDTOAsync(discount);
        }

        // Saldo a favor todavía pendiente en una tarjeta (D9): no se guarda, se calcula.
        public async Task<CardPendingCreditDTO> GetPendingOnCardAsync(int userId, int cardId)
        {
            var discounts = await _cardTransactionDiscountRepository.GetPendingOnCardAsync(cardId, userId);

            var items = discounts.Select(d => new CardPendingCreditItemDTO
            {
                DiscountId = d.Id,
                CardTransactionId = d.CardTransactionId,
                Detail = d.CardTransaction?.Detail,
                CreditDate = d.CreditDate,
                Pending = d.Amount - d.AmountMaterialized
            }).ToList();

            return new CardPendingCreditDTO
            {
                CardId = cardId,
                TotalPending = items.Sum(i => i.Pending),
                Items = items
            };
        }

        private async Task<CardTransactionDiscountDetailDTO> MapToDetailDTOAsync(CardTransactionDiscount discount)
        {
            var installments = await _cardTransactionDiscountRepository.GetInstallmentsByDiscountIdAsync(discount.Id);

            return new CardTransactionDiscountDetailDTO
            {
                Id = discount.Id,
                CardTransactionId = discount.CardTransactionId,
                Amount = discount.Amount,
                AmountApplied = discount.AmountApplied,
                AmountMaterialized = discount.AmountMaterialized,
                PendingOnCard = discount.Amount - discount.AmountMaterialized,
                CreditTarget = discount.CreditTarget,
                CreditDate = discount.CreditDate,
                Notes = discount.Notes,
                Installments = installments
                    .Select(i => new CardTransactionDiscountInstallmentDTO { InstallmentNumber = i.InstallmentNumber, Amount = i.Amount })
                    .ToList()
            };
        }
    }
}
