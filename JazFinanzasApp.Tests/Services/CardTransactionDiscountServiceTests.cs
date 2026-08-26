using JazFinanzasApp.API.Business.Interfaces;
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
        private readonly Mock<ICardPaymentRepository> _cardPaymentRepoMock;
        private readonly Mock<IQuotePriceResolver> _quotePriceResolverMock;
        private readonly CardTransactionDiscountService _sut;

        private const int UserId = 1;
        private const int CardId = 7;

        public CardTransactionDiscountServiceTests()
        {
            _discountRepoMock = new Mock<ICardTransactionDiscountRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _cardPaymentRepoMock = new Mock<ICardPaymentRepository>();
            _quotePriceResolverMock = new Mock<IQuotePriceResolver>();
            _quotePriceResolverMock.Setup(r => r.ResolveAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
                .ReturnsAsync(1000m);

            _sut = new CardTransactionDiscountService(
                _discountRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _accountRepoMock.Object,
                _transactionClassRepoMock.Object,
                _transactionRepoMock.Object,
                _portfolioRepoMock.Object,
                _cardPaymentRepoMock.Object,
                _quotePriceResolverMock.Object);
        }

        private CardTransaction MakeCardTransaction(int installments = 6, decimal totalAmount = 1200m) => new()
        {
            Id = 20,
            UserId = UserId,
            AssetId = 1,
            CardId = CardId,
            Detail = "Compra",
            TotalAmount = totalAmount,
            Installments = installments,
            FirstInstallment = new DateTime(2026, 1, 1),
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
                CreditTarget = CardTransactionDiscountCreditTarget.Account,
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

            var dto = new CardTransactionDiscountAddDTO { CardTransactionId = 20, Amount = 100m, CreditTarget = CardTransactionDiscountCreditTarget.Account, AccountId = 2, Date = DateTime.Today };

            await FluentActions.Invoking(() => _sut.CreateAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateAsync_WhenCardTransactionBelongsToAnotherUser_ThrowsUnauthorizedDomainException()
        {
            var cardTransaction = MakeCardTransaction();
            cardTransaction.UserId = 999;
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);

            var dto = new CardTransactionDiscountAddDTO { CardTransactionId = 20, Amount = 100m, CreditTarget = CardTransactionDiscountCreditTarget.Account, AccountId = 2, Date = DateTime.Today };

            await FluentActions.Invoking(() => _sut.CreateAsync(UserId, dto))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task CreateAsync_WhenAmountExceedsCardTransactionTotal_ThrowsBusinessRuleException()
        {
            var cardTransaction = MakeCardTransaction(totalAmount: 100m);
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);
            _discountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((CardTransactionDiscount?)null);

            var dto = new CardTransactionDiscountAddDTO { CardTransactionId = 20, Amount = 200m, CreditTarget = CardTransactionDiscountCreditTarget.Account, AccountId = 2, Date = DateTime.Today };

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

        // ── GetActiveByUserIdAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetActiveByUserIdAsync_ReturnsOnlyDiscountsWithRemainingAmount()
        {
            var discounts = new List<CardTransactionDiscount>
            {
                new() { Id = 1, CardTransactionId = 20, Amount = 200m, AmountApplied = 50m }
            };
            _discountRepoMock.Setup(r => r.GetActiveByUserIdAsync(UserId)).ReturnsAsync(discounts);

            var result = (await _sut.GetActiveByUserIdAsync(UserId)).ToList();

            result.Should().ContainSingle();
            result[0].CardTransactionId.Should().Be(20);
            result[0].Amount.Should().Be(200m);
            result[0].AmountApplied.Should().Be(50m);
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
    
        // -- MaterializeAsync (Fase 2) --------------------------------------

        private CardTransactionDiscount MakeDiscount(decimal amount, decimal materialized = 0m) => new()
        {
            Id = 1,
            CardTransactionId = 20,
            UserId = UserId,
            Amount = amount,
            AmountApplied = 0m,
            AmountMaterialized = materialized,
            CreditTarget = CardTransactionDiscountCreditTarget.Card,
            CreditDate = new DateTime(2026, 1, 5)
        };

        private List<CardTransactionDiscountInstallment> TrackInstallments()
        {
            var creadas = new List<CardTransactionDiscountInstallment>();
            _discountRepoMock.Setup(r => r.AddInstallmentAsync(It.IsAny<CardTransactionDiscountInstallment>()))
                .Callback<CardTransactionDiscountInstallment>(i => creadas.Add(i))
                .Returns(Task.CompletedTask);
            // Lo ya etiquetado se relee en cada materializacion para calcular el tope de cada cuota.
            _discountRepoMock.Setup(r => r.GetInstallmentsByDiscountIdAsync(1))
                .ReturnsAsync(() => creadas.ToList());
            return creadas;
        }

        [Fact]
        public async Task MaterializeAsync_CalledTwice_AccumulatesOnSameInstallmentWithoutExceedingItsCap()
        {
            // Tarjeta $1200 en 6 cuotas ($200/cuota). Se acredita en dos tandas: $100 y despues $260.
            // La cuota 1 ya tenia $100, asi que solo puede absorber $100 mas; el resto cae en la cuota 2.
            var cardTransaction = MakeCardTransaction(installments: 6, totalAmount: 1200m);
            SetupHappyPathDependencies(cardTransaction);
            var creadas = TrackInstallments();
            var discount = MakeDiscount(amount: 360m);

            await _sut.MaterializeAsync(discount, 100m, accountId: 2, new DateTime(2026, 1, 5), UserId);
            await _sut.MaterializeAsync(discount, 260m, accountId: 2, new DateTime(2026, 2, 5), UserId);

            creadas.Should().HaveCount(3);
            creadas[0].InstallmentNumber.Should().Be(1);
            creadas[0].Amount.Should().Be(100m);
            creadas[1].InstallmentNumber.Should().Be(1);
            creadas[1].Amount.Should().Be(100m);
            creadas[2].InstallmentNumber.Should().Be(2);
            creadas[2].Amount.Should().Be(160m);
            creadas.Where(i => i.InstallmentNumber == 1).Sum(i => i.Amount).Should().Be(200m);
            discount.AmountMaterialized.Should().Be(360m);
        }

        [Fact]
        public async Task MaterializeAsync_WhenFirstInstallmentAlreadyPaid_StartsAtTheNextUnpaidOne()
        {
            // El banco acredito tarde: la cuota 1 ya se pago a precio pleno, asi que el descuento
            // no tiene que intentar meterse ahi.
            var cardTransaction = MakeCardTransaction(installments: 6, totalAmount: 1200m);
            SetupHappyPathDependencies(cardTransaction);
            _cardPaymentRepoMock.Setup(r => r.GetPaidMonthsAsync(CardId))
                .ReturnsAsync(new[] { new DateTime(2026, 1, 1) });
            var creadas = TrackInstallments();
            var discount = MakeDiscount(amount: 360m);

            await _sut.MaterializeAsync(discount, 360m, accountId: 2, new DateTime(2026, 2, 5), UserId);

            creadas.Should().HaveCount(2);
            creadas[0].InstallmentNumber.Should().Be(2);
            creadas[0].Amount.Should().Be(200m);
            creadas[1].InstallmentNumber.Should().Be(3);
            creadas[1].Amount.Should().Be(160m);
        }

        [Fact]
        public async Task MaterializeAsync_WhenNoUnpaidInstallmentsLeft_ThrowsAndCreatesNothing()
        {
            // Un solo pago, ya hecho: no queda donde aplicar el descuento. Preserva el invariante de
            // que ningun reintegro queda vivo para siempre -- mejor rechazar que crear un ingreso huerfano.
            var cardTransaction = MakeCardTransaction(installments: 1, totalAmount: 200m);
            SetupHappyPathDependencies(cardTransaction);
            _cardPaymentRepoMock.Setup(r => r.GetPaidMonthsAsync(CardId))
                .ReturnsAsync(new[] { new DateTime(2026, 1, 1) });
            var creadas = TrackInstallments();
            var discount = MakeDiscount(amount: 100m);

            await FluentActions.Invoking(() => _sut.MaterializeAsync(discount, 100m, 2, new DateTime(2026, 2, 5), UserId))
                .Should().ThrowAsync<BusinessRuleException>();

            creadas.Should().BeEmpty();
            _transactionRepoMock.Verify(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()), Times.Never);
            discount.AmountMaterialized.Should().Be(0m);
        }

        [Fact]
        public async Task MaterializeAsync_WithAmountAboveWhatIsPending_ThrowsBusinessRuleException()
        {
            var cardTransaction = MakeCardTransaction(installments: 6, totalAmount: 1200m);
            SetupHappyPathDependencies(cardTransaction);
            var discount = MakeDiscount(amount: 360m, materialized: 300m);

            await FluentActions.Invoking(() => _sut.MaterializeAsync(discount, 100m, 2, new DateTime(2026, 2, 5), UserId))
                .Should().ThrowAsync<BusinessRuleException>();

            discount.AmountMaterialized.Should().Be(300m);
        }

        // -- Modalidad tarjeta y rescate (Fase 3) ---------------------------

        [Fact]
        public async Task CreateAsync_WithCreditOnCard_CreatesNoTransactionsAndLeavesItAllPendingOnTheCard()
        {
            // La plata esta en la tarjeta, no en una cuenta: no hay ningun movimiento que registrar todavia.
            var cardTransaction = MakeCardTransaction(installments: 5, totalAmount: 100000m);
            SetupHappyPathDependencies(cardTransaction);

            var dto = new CardTransactionDiscountAddDTO
            {
                CardTransactionId = 20,
                Amount = 35000m,
                CreditTarget = CardTransactionDiscountCreditTarget.Card,
                AccountId = null,
                Date = new DateTime(2026, 1, 5)
            };

            var result = await _sut.CreateAsync(UserId, dto);

            result.CreditTarget.Should().Be(CardTransactionDiscountCreditTarget.Card);
            result.AmountMaterialized.Should().Be(0m);
            result.PendingOnCard.Should().Be(35000m);
            result.Installments.Should().BeEmpty();
            _transactionRepoMock.Verify(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()), Times.Never);
            _discountRepoMock.Verify(r => r.AddInstallmentAsync(It.IsAny<CardTransactionDiscountInstallment>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithCreditOnAccountButNoAccount_ThrowsBusinessRuleException()
        {
            var cardTransaction = MakeCardTransaction();
            SetupHappyPathDependencies(cardTransaction);

            var dto = new CardTransactionDiscountAddDTO
            {
                CardTransactionId = 20,
                Amount = 100m,
                CreditTarget = CardTransactionDiscountCreditTarget.Account,
                AccountId = null,
                Date = DateTime.Today
            };

            await FluentActions.Invoking(() => _sut.CreateAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateAsync_WithoutCreditTarget_FallsBackToAccountMode()
        {
            // Retrocompatibilidad: el frontend no manda el campo hasta la Fase 6, y hasta entonces
            // tiene que seguir cargando promociones como siempre.
            var cardTransaction = MakeCardTransaction(installments: 6, totalAmount: 1200m);
            SetupHappyPathDependencies(cardTransaction);
            var creadas = TrackInstallments();

            var dto = new CardTransactionDiscountAddDTO
            {
                CardTransactionId = 20,
                Amount = 200m,
                CreditTarget = null,
                AccountId = 2,
                Date = new DateTime(2026, 1, 1)
            };

            var result = await _sut.CreateAsync(UserId, dto);

            result.CreditTarget.Should().Be(CardTransactionDiscountCreditTarget.Account);
            result.AmountMaterialized.Should().Be(200m);
            creadas.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateAsync_WithUnknownCreditTarget_ThrowsBusinessRuleException()
        {
            var cardTransaction = MakeCardTransaction();
            SetupHappyPathDependencies(cardTransaction);

            var dto = new CardTransactionDiscountAddDTO
            {
                CardTransactionId = 20,
                Amount = 100m,
                CreditTarget = "OTRA_COSA",
                AccountId = 2,
                Date = DateTime.Today
            };

            await FluentActions.Invoking(() => _sut.CreateAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task RescueAsync_WithPartialAmount_MaterializesOnlyThatAndLeavesTheRestOnTheCard()
        {
            // Compra $100.000 en 5 cuotas de $20.000, reintegro de $35.000 sobre la tarjeta.
            // Se rescatan $20.000: entran enteros en la cuota 1 y quedan $15.000 en la tarjeta.
            var cardTransaction = MakeCardTransaction(installments: 5, totalAmount: 100000m);
            SetupHappyPathDependencies(cardTransaction);
            var creadas = TrackInstallments();
            var discount = MakeDiscount(amount: 35000m);
            _discountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(discount);

            var dto = new CardTransactionDiscountRescueDTO { Amount = 20000m, AccountId = 2, Date = new DateTime(2026, 2, 10) };
            var result = await _sut.RescueAsync(UserId, 1, dto);

            creadas.Should().HaveCount(1);
            creadas[0].InstallmentNumber.Should().Be(1);
            creadas[0].Amount.Should().Be(20000m);
            result.AmountMaterialized.Should().Be(20000m);
            result.PendingOnCard.Should().Be(15000m);
        }

        [Fact]
        public async Task RescueAsync_ForMoreThanWhatIsPendingOnTheCard_ThrowsBusinessRuleException()
        {
            var cardTransaction = MakeCardTransaction(installments: 5, totalAmount: 100000m);
            SetupHappyPathDependencies(cardTransaction);
            var discount = MakeDiscount(amount: 35000m, materialized: 30000m);
            _discountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(discount);

            var dto = new CardTransactionDiscountRescueDTO { Amount = 10000m, AccountId = 2, Date = new DateTime(2026, 2, 10) };

            await FluentActions.Invoking(() => _sut.RescueAsync(UserId, 1, dto))
                .Should().ThrowAsync<BusinessRuleException>();
            discount.AmountMaterialized.Should().Be(30000m);
        }

        [Fact]
        public async Task RescueAsync_OnAnAccountModeDiscount_ThrowsBusinessRuleException()
        {
            // No hay nada que rescatar: esa plata nunca estuvo en la tarjeta.
            var discount = MakeDiscount(amount: 1000m);
            discount.CreditTarget = CardTransactionDiscountCreditTarget.Account;
            _discountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(discount);

            var dto = new CardTransactionDiscountRescueDTO { Amount = 100m, AccountId = 2, Date = DateTime.Today };

            await FluentActions.Invoking(() => _sut.RescueAsync(UserId, 1, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task GetPendingOnCardAsync_SumsWhatIsLeftOnEachDiscountOfTheCard()
        {
            var d1 = MakeDiscount(amount: 35000m, materialized: 20000m);
            d1.CardTransaction = new CardTransaction { Id = 20, Detail = "Compra grande" };
            var d2 = MakeDiscount(amount: 5000m);
            d2.Id = 2;
            d2.CardTransactionId = 21;
            d2.CardTransaction = new CardTransaction { Id = 21, Detail = "Otra compra" };

            _discountRepoMock.Setup(r => r.GetPendingOnCardAsync(CardId, UserId))
                .ReturnsAsync(new[] { d1, d2 });

            var result = await _sut.GetPendingOnCardAsync(UserId, CardId);

            result.TotalPending.Should().Be(20000m);
            result.Items.Should().HaveCount(2);
            result.Items[0].Pending.Should().Be(15000m);
            result.Items[0].Detail.Should().Be("Compra grande");
            result.Items[1].Pending.Should().Be(5000m);
        }
}
}
