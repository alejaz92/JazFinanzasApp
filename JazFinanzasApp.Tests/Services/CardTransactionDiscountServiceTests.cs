using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.CardTransactionDiscount;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class CardTransactionDiscountServiceTests
    {
        private readonly Mock<ICardTransactionDiscountRepository> _discountRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly CardTransactionDiscountService _sut;

        private const int UserId = 1;

        public CardTransactionDiscountServiceTests()
        {
            _discountRepoMock = new Mock<ICardTransactionDiscountRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();

            _sut = new CardTransactionDiscountService(
                _discountRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _accountRepoMock.Object,
                _transactionClassRepoMock.Object,
                _transactionRepoMock.Object,
                _portfolioRepoMock.Object);
        }

        private CardTransaction MakeCardTransaction(int installments = 6, decimal totalAmount = 1200m) => new()
        {
            Id = 20,
            UserId = UserId,
            AssetId = 1,
            Detail = "Compra",
            TotalAmount = totalAmount,
            Installments = installments,
            InstallmentAmount = totalAmount / installments
        };

        private void SetupHappyPathDependencies(CardTransaction cardTransaction)
        {
            var account = new Account { Id = 2, UserId = UserId };
            var transactionClass = new TransactionClass { Id = 3, UserId = UserId, Description = "Reintegro", IncExp = "I", IsSystem = true };
            var portfolio = new Portfolio { Id = 1, UserId = UserId, IsDefault = true };

            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(cardTransaction.Id)).ReturnsAsync(cardTransaction);
            _discountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(cardTransaction.Id)).ReturnsAsync((CardTransactionDiscount?)null);
            _accountRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(account);
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Reintegro", UserId)).ReturnsAsync(transactionClass);
            _portfolioRepoMock.Setup(r => r.GetDefaultPortfolio(UserId)).ReturnsAsync(portfolio);
            _discountRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<CardTransactionDiscount>()))
                .ReturnsAsync((CardTransactionDiscount d) => { d.Id = 1; return d; });
            _transactionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = new Random().Next(1000, 9999); return t; });
        }

        // ── CreateAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_WithAmountSpanningTwoInstallments_CreatesTwoInstallmentsFifo()
        {
            // Calcado del ejemplo de verificación del plan: tarjeta $1200 en 6 cuotas ($200/cuota),
            // descuento de $360 -> cuota 1 absorbe $200, cuota 2 absorbe el remanente $160.
            var cardTransaction = MakeCardTransaction(installments: 6, totalAmount: 1200m);
            SetupHappyPathDependencies(cardTransaction);

            var dto = new CardTransactionDiscountAddDTO
            {
                CardTransactionId = 20,
                Amount = 360m,
                AccountId = 2,
                Date = new DateTime(2026, 1, 1)
            };

            var createdInstallments = new List<CardTransactionDiscountInstallment>();
            _discountRepoMock.Setup(r => r.AddInstallmentAsync(It.IsAny<CardTransactionDiscountInstallment>()))
                .Callback<CardTransactionDiscountInstallment>(i => createdInstallments.Add(i))
                .Returns(Task.CompletedTask);

            var result = await _sut.CreateAsync(UserId, dto);

            result.Amount.Should().Be(360m);
            result.AmountApplied.Should().Be(0m);
            createdInstallments.Should().HaveCount(2);
            createdInstallments[0].InstallmentNumber.Should().Be(1);
            createdInstallments[0].Amount.Should().Be(200m);
            createdInstallments[1].InstallmentNumber.Should().Be(2);
            createdInstallments[1].Amount.Should().Be(160m);
            _transactionRepoMock.Verify(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateAsync_WhenDiscountAlreadyExistsForCardTransaction_ThrowsBusinessRuleException()
        {
            var cardTransaction = MakeCardTransaction();
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);
            _discountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20))
                .ReturnsAsync(new CardTransactionDiscount { Id = 5, CardTransactionId = 20 });

            var dto = new CardTransactionDiscountAddDTO { CardTransactionId = 20, Amount = 100m, AccountId = 2, Date = DateTime.Today };

            await FluentActions.Invoking(() => _sut.CreateAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateAsync_WhenCardTransactionBelongsToAnotherUser_ThrowsUnauthorizedDomainException()
        {
            var cardTransaction = MakeCardTransaction();
            cardTransaction.UserId = 999;
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);

            var dto = new CardTransactionDiscountAddDTO { CardTransactionId = 20, Amount = 100m, AccountId = 2, Date = DateTime.Today };

            await FluentActions.Invoking(() => _sut.CreateAsync(UserId, dto))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task CreateAsync_WhenAmountExceedsCardTransactionTotal_ThrowsBusinessRuleException()
        {
            var cardTransaction = MakeCardTransaction(totalAmount: 100m);
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);
            _discountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((CardTransactionDiscount?)null);

            var dto = new CardTransactionDiscountAddDTO { CardTransactionId = 20, Amount = 200m, AccountId = 2, Date = DateTime.Today };

            await FluentActions.Invoking(() => _sut.CreateAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        // ── GetByCardTransactionIdAsync ───────────────────────────────────────

        [Fact]
        public async Task GetByCardTransactionIdAsync_WithExistingDiscount_ReturnsDetail()
        {
            var cardTransaction = MakeCardTransaction();
            var discount = new CardTransactionDiscount { Id = 1, CardTransactionId = 20, Amount = 300m, AmountApplied = 50m };

            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);
            _discountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync(discount);

            var result = await _sut.GetByCardTransactionIdAsync(UserId, 20);

            result.Id.Should().Be(1);
            result.Amount.Should().Be(300m);
            result.AmountApplied.Should().Be(50m);
        }

        // ── DeleteAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_WhenAmountAppliedIsZero_DeletesDiscountAndInstallments()
        {
            var discount = new CardTransactionDiscount { Id = 1, UserId = UserId, AmountApplied = 0 };
            var installments = new List<CardTransactionDiscountInstallment>
            {
                new() { Id = 10, CardTransactionDiscountId = 1, TransactionId = 100 }
            };

            _discountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(discount);
            _discountRepoMock.Setup(r => r.GetInstallmentsByDiscountIdAsync(1)).ReturnsAsync(installments);

            await _sut.DeleteAsync(UserId, 1);

            _discountRepoMock.Verify(r => r.DeleteInstallmentAsync(10), Times.Once);
            _transactionRepoMock.Verify(r => r.DeleteAsync(100), Times.Once);
            _discountRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenAmountAppliedIsGreaterThanZero_ThrowsBusinessRuleException()
        {
            var discount = new CardTransactionDiscount { Id = 1, UserId = UserId, AmountApplied = 50m };
            _discountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(discount);

            await FluentActions.Invoking(() => _sut.DeleteAsync(UserId, 1))
                .Should().ThrowAsync<BusinessRuleException>();

            _discountRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
