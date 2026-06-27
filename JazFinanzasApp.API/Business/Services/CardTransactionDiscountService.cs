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

        public CardTransactionDiscountService(
            ICardTransactionDiscountRepository cardTransactionDiscountRepository,
            ICardTransactionRepository cardTransactionRepository,
            IAccountRepository accountRepository,
            ITransactionClassRepository transactionClassRepository,
            ITransactionRepository transactionRepository,
            IPortfolioRepository portfolioRepository)
        {
            _cardTransactionDiscountRepository = cardTransactionDiscountRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _accountRepository = accountRepository;
            _transactionClassRepository = transactionClassRepository;
            _transactionRepository = transactionRepository;
            _portfolioRepository = portfolioRepository;
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

            var account = await _accountRepository.GetByIdAsync(dto.AccountId)
                ?? throw new NotFoundException("Cuenta no encontrada");
            if (account.UserId != userId)
                throw new UnauthorizedDomainException();

            var transactionClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Reintegro", userId)
                ?? throw new NotFoundException("Clase de transacción 'Reintegro' no encontrada");

            var defaultPortfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                ?? throw new NotFoundException("Portfolio por defecto no encontrado");

            var discount = await _cardTransactionDiscountRepository.AddAsyncReturnObject(new CardTransactionDiscount
            {
                CardTransactionId = dto.CardTransactionId,
                Amount = dto.Amount,
                AmountApplied = 0,
                Notes = dto.Notes,
                UserId = userId
            });

            // FIFO: el descuento se pre-particiona por cuota exacta al crearse, topando cada cuota
            // a lo que realmente cuesta esa cuota de la tarjeta (no a Amount/Installments del propio descuento).
            decimal remaining = dto.Amount;
            int installmentNumber = 1;

            while (remaining > 0 && installmentNumber <= cardTransaction.Installments)
            {
                var portion = Math.Min(remaining, cardTransaction.InstallmentAmount);

                var incomeTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = account.Id,
                    Account = account,
                    PortfolioId = defaultPortfolio.Id,
                    Portfolio = defaultPortfolio,
                    AssetId = cardTransaction.AssetId,
                    Date = dto.Date,
                    MovementType = "I",
                    TransactionClassId = transactionClass.Id,
                    TransactionClass = transactionClass,
                    Detail = $"Descuento - {cardTransaction.Detail}",
                    Amount = portion,
                    UserId = userId
                });

                await _cardTransactionDiscountRepository.AddInstallmentAsync(new CardTransactionDiscountInstallment
                {
                    CardTransactionDiscountId = discount.Id,
                    TransactionId = incomeTransaction.Id,
                    Amount = portion,
                    InstallmentNumber = installmentNumber,
                    Date = dto.Date
                });

                remaining -= portion;
                installmentNumber++;
            }

            return await MapToDetailDTOAsync(discount);
        }

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

        private async Task<CardTransactionDiscountDetailDTO> MapToDetailDTOAsync(CardTransactionDiscount discount)
        {
            var installments = await _cardTransactionDiscountRepository.GetInstallmentsByDiscountIdAsync(discount.Id);

            return new CardTransactionDiscountDetailDTO
            {
                Id = discount.Id,
                CardTransactionId = discount.CardTransactionId,
                Amount = discount.Amount,
                AmountApplied = discount.AmountApplied,
                Notes = discount.Notes,
                Installments = installments
                    .Select(i => new CardTransactionDiscountInstallmentDTO { InstallmentNumber = i.InstallmentNumber, Amount = i.Amount })
                    .ToList()
            };
        }
    }
}
