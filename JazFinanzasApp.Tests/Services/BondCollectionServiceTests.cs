using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.BondCollection;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class BondCollectionServiceTests
    {
        private readonly Mock<IBondPaymentRepository> _bondPaymentRepoMock;
        private readonly Mock<IBondCollectionRepository> _bondCollectionRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<IQuotePriceResolver> _quotePriceResolverMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly BondCollectionService _sut;

        private const int UserId = 1;

        public BondCollectionServiceTests()
        {
            _bondPaymentRepoMock = new Mock<IBondPaymentRepository>();
            _bondCollectionRepoMock = new Mock<IBondCollectionRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _quotePriceResolverMock = new Mock<IQuotePriceResolver>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _sut = new BondCollectionService(
                _bondPaymentRepoMock.Object,
                _bondCollectionRepoMock.Object,
                _transactionRepoMock.Object,
                _accountRepoMock.Object,
                _portfolioRepoMock.Object,
                _transactionClassRepoMock.Object,
                _quotePriceResolverMock.Object,
                _unitOfWorkMock.Object);
        }

        private static BondPayment MakePayment(int id = 1, decimal interestPer100 = 8m, decimal amortizationRate = 0.08m, bool isIndexed = false) =>
            new()
            {
                Id = id,
                AssetId = 48,
                PaymentDate = new DateTime(2026, 7, 9),
                InterestPer100 = interestPer100,
                AmortizationRate = amortizationRate,
                IsIndexed = isIndexed,
                Asset = new Asset { Id = 48, Name = "Bonos Rep. Arg. USD Step Up 2030", Symbol = "AL30" },
                CurrencyAsset = new Asset { Id = 2, Symbol = "USD" }
            };

        private static BondHoldingResult MakeHolding(int accountId = 1, int portfolioId = 1, decimal quantity = 5m) =>
            new() { AccountId = accountId, AccountName = "Bull Market", PortfolioId = portfolioId, PortfolioName = "Default", Quantity = quantity };

        // ── BuildPendingItem (pura) ────────────────────────────────────────────

        [Fact]
        public void BuildPendingItem_WithAmortization_EstimatesCapitalAndInterest()
        {
            var payment = MakePayment(interestPer100: 0.27m, amortizationRate: 0.08m);
            var holding = MakeHolding(quantity: 4.93m);

            var result = BondCollectionService.BuildPendingItem(payment, holding);

            result.EstimatedCapital.Should().Be(39.44m); // 4.93 * 100 * 0.08
            result.EstimatedInterest.Should().Be(1.33m); // 4.93 * 0.27
            result.IsIndexed.Should().BeFalse();
            result.CurrencySymbol.Should().Be("USD");
        }

        [Fact]
        public void BuildPendingItem_WithoutAmortizationThisPeriod_EstimatesOnlyInterest()
        {
            var payment = MakePayment(interestPer100: 2.0625m, amortizationRate: 0m);
            var holding = MakeHolding(quantity: 0.54m);

            var result = BondCollectionService.BuildPendingItem(payment, holding);

            result.EstimatedCapital.Should().Be(0m);
            result.EstimatedInterest.Should().Be(1.11m); // 0.54 * 2.0625 = 1.11375 -> 1.11
        }

        [Fact]
        public void BuildPendingItem_WhenIndexed_DoesNotEstimateAmounts()
        {
            var payment = MakePayment(isIndexed: true);
            var holding = MakeHolding();

            var result = BondCollectionService.BuildPendingItem(payment, holding);

            result.EstimatedCapital.Should().BeNull();
            result.EstimatedInterest.Should().BeNull();
            result.IsIndexed.Should().BeTrue();
        }

        // ── BuildPending (pura) ─────────────────────────────────────────────────

        [Fact]
        public void BuildPending_WithNoResolvedRecord_ReturnsPendingItem()
        {
            var payment = MakePayment(id: 10);
            var holding = MakeHolding();
            var holdingsByPayment = new Dictionary<int, List<BondHoldingResult>> { [10] = new() { holding } };
            var resolvedKeys = new HashSet<(int, int, int)>();

            var result = BondCollectionService.BuildPending(new[] { payment }, holdingsByPayment, resolvedKeys);

            result.Should().ContainSingle();
            result[0].BondPaymentId.Should().Be(10);
        }

        [Fact]
        public void BuildPending_WhenAlreadyResolved_ExcludesItRegardlessOfStatus()
        {
            var payment = MakePayment(id: 10);
            var holding = MakeHolding(accountId: 5, portfolioId: 7);
            var holdingsByPayment = new Dictionary<int, List<BondHoldingResult>> { [10] = new() { holding } };
            var resolvedKeys = new HashSet<(int, int, int)> { (10, 5, 7) };

            var result = BondCollectionService.BuildPending(new[] { payment }, holdingsByPayment, resolvedKeys);

            result.Should().BeEmpty();
        }

        [Fact]
        public void BuildPending_WithoutHolding_ReturnsNothingForThatPayment()
        {
            var payment = MakePayment(id: 10);
            var holdingsByPayment = new Dictionary<int, List<BondHoldingResult>>(); // sin tenencia

            var result = BondCollectionService.BuildPending(new[] { payment }, holdingsByPayment, new HashSet<(int, int, int)>());

            result.Should().BeEmpty();
        }

        // ── GetPendingAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetPendingAsync_WithHoldingAndNoRecord_ReturnsOneItem()
        {
            var payment = MakePayment(id: 10);
            _bondPaymentRepoMock.Setup(r => r.GetPastAsync(It.IsAny<DateTime>())).ReturnsAsync(new[] { payment });
            _transactionRepoMock.Setup(r => r.GetBondHoldingsBeforeDateAsync(UserId, 48, payment.PaymentDate))
                .ReturnsAsync(new[] { MakeHolding() });
            _bondCollectionRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(Enumerable.Empty<BondCollection>());

            var result = await _sut.GetPendingAsync(UserId);

            result.Should().ContainSingle();
        }

        [Fact]
        public async Task GetPendingAsync_WithoutHolding_ReturnsEmpty()
        {
            var payment = MakePayment(id: 10);
            _bondPaymentRepoMock.Setup(r => r.GetPastAsync(It.IsAny<DateTime>())).ReturnsAsync(new[] { payment });
            _transactionRepoMock.Setup(r => r.GetBondHoldingsBeforeDateAsync(UserId, 48, payment.PaymentDate))
                .ReturnsAsync(Enumerable.Empty<BondHoldingResult>());
            _bondCollectionRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(Enumerable.Empty<BondCollection>());

            var result = await _sut.GetPendingAsync(UserId);

            result.Should().BeEmpty();
        }

        // ── RegisterAsync ────────────────────────────────────────────────────────

        private void SetupHappyPathForRegister(BondPayment payment, BondHoldingResult holding, TransactionClass interestClass, TransactionClass capitalClass)
        {
            _bondPaymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
            _accountRepoMock.Setup(r => r.GetByIdAsync(holding.AccountId)).ReturnsAsync(new Account { Id = holding.AccountId, UserId = UserId });
            _portfolioRepoMock.Setup(r => r.GetByIdAsync(holding.PortfolioId)).ReturnsAsync(new Portfolio { Id = holding.PortfolioId, UserId = UserId });
            _transactionRepoMock.Setup(r => r.GetBondHoldingsBeforeDateAsync(UserId, payment.AssetId, payment.PaymentDate))
                .ReturnsAsync(new[] { holding });
            _bondCollectionRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(Enumerable.Empty<BondCollection>());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(interestClass.Id)).ReturnsAsync(interestClass);
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Ingreso Inversiones", UserId)).ReturnsAsync(capitalClass);
            _quotePriceResolverMock.Setup(r => r.ResolveAsync(payment.CurrencyAssetId, It.IsAny<DateTime>())).ReturnsAsync(1m);
            _bondCollectionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<BondCollection>()))
                .ReturnsAsync((BondCollection c) => { c.Id = 99; return c; });
        }

        [Fact]
        public async Task RegisterAsync_WithCapitalAndInterest_CreatesTwoTransactions()
        {
            var payment = MakePayment(id: 10);
            var holding = MakeHolding();
            var interestClass = new TransactionClass { Id = 5, UserId = UserId, IncExp = "I", CountsAsIncomeExpense = true };
            var capitalClass = new TransactionClass { Id = 6, UserId = UserId, IncExp = "I", CountsAsIncomeExpense = false };
            SetupHappyPathForRegister(payment, holding, interestClass, capitalClass);

            var dto = new BondCollectionRegisterDTO
            {
                BondPaymentId = payment.Id,
                AccountId = holding.AccountId,
                PortfolioId = holding.PortfolioId,
                CollectionDate = payment.PaymentDate,
                CapitalAmount = 39.44m,
                InterestAmount = 1.33m,
                InterestTransactionClassId = interestClass.Id
            };

            var id = await _sut.RegisterAsync(UserId, dto);

            id.Should().Be(99);
            _transactionRepoMock.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.TransactionClassId == capitalClass.Id && t.Amount == 39.44m)), Times.Once);
            _transactionRepoMock.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.TransactionClassId == interestClass.Id && t.Amount == 1.33m)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithOnlyCapital_CreatesOneTransaction()
        {
            var payment = MakePayment(id: 10);
            var holding = MakeHolding();
            var capitalClass = new TransactionClass { Id = 6, UserId = UserId, IncExp = "I", CountsAsIncomeExpense = false };
            _bondPaymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
            _accountRepoMock.Setup(r => r.GetByIdAsync(holding.AccountId)).ReturnsAsync(new Account { Id = holding.AccountId, UserId = UserId });
            _portfolioRepoMock.Setup(r => r.GetByIdAsync(holding.PortfolioId)).ReturnsAsync(new Portfolio { Id = holding.PortfolioId, UserId = UserId });
            _transactionRepoMock.Setup(r => r.GetBondHoldingsBeforeDateAsync(UserId, payment.AssetId, payment.PaymentDate)).ReturnsAsync(new[] { holding });
            _bondCollectionRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(Enumerable.Empty<BondCollection>());
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Ingreso Inversiones", UserId)).ReturnsAsync(capitalClass);
            _quotePriceResolverMock.Setup(r => r.ResolveAsync(payment.CurrencyAssetId, It.IsAny<DateTime>())).ReturnsAsync(1m);
            _bondCollectionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<BondCollection>())).ReturnsAsync((BondCollection c) => { c.Id = 99; return c; });

            var dto = new BondCollectionRegisterDTO
            {
                BondPaymentId = payment.Id,
                AccountId = holding.AccountId,
                PortfolioId = holding.PortfolioId,
                CollectionDate = payment.PaymentDate,
                CapitalAmount = 39.44m,
                InterestAmount = 0m
            };

            await _sut.RegisterAsync(UserId, dto);

            _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            _transactionClassRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WhenBothAmountsAreZero_ThrowsBusinessRuleException()
        {
            var dto = new BondCollectionRegisterDTO { BondPaymentId = 1, AccountId = 1, PortfolioId = 1, CapitalAmount = 0, InterestAmount = 0 };

            var act = () => _sut.RegisterAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task RegisterAsync_WhenPaymentDateIsInTheFuture_ThrowsBusinessRuleException()
        {
            var payment = MakePayment(id: 10);
            payment.PaymentDate = DateTime.Today.AddDays(5);
            _bondPaymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);

            var dto = new BondCollectionRegisterDTO { BondPaymentId = payment.Id, AccountId = 1, PortfolioId = 1, CapitalAmount = 10m };

            var act = () => _sut.RegisterAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task RegisterAsync_WhenAccountBelongsToAnotherUser_ThrowsUnauthorized()
        {
            var payment = MakePayment(id: 10);
            _bondPaymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
            _accountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account { Id = 1, UserId = 999 });

            var dto = new BondCollectionRegisterDTO { BondPaymentId = payment.Id, AccountId = 1, PortfolioId = 1, CapitalAmount = 10m };

            var act = () => _sut.RegisterAsync(UserId, dto);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task RegisterAsync_WhenNoHoldingAtAccountAndPortfolio_ThrowsBusinessRuleException()
        {
            var payment = MakePayment(id: 10);
            _bondPaymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
            _accountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account { Id = 1, UserId = UserId });
            _portfolioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Portfolio { Id = 1, UserId = UserId });
            _transactionRepoMock.Setup(r => r.GetBondHoldingsBeforeDateAsync(UserId, payment.AssetId, payment.PaymentDate))
                .ReturnsAsync(Enumerable.Empty<BondHoldingResult>());

            var dto = new BondCollectionRegisterDTO { BondPaymentId = payment.Id, AccountId = 1, PortfolioId = 1, CapitalAmount = 10m };

            var act = () => _sut.RegisterAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task RegisterAsync_WhenAlreadyRegisteredForSameCombination_ThrowsBusinessRuleException()
        {
            var payment = MakePayment(id: 10);
            var holding = MakeHolding();
            _bondPaymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
            _accountRepoMock.Setup(r => r.GetByIdAsync(holding.AccountId)).ReturnsAsync(new Account { Id = holding.AccountId, UserId = UserId });
            _portfolioRepoMock.Setup(r => r.GetByIdAsync(holding.PortfolioId)).ReturnsAsync(new Portfolio { Id = holding.PortfolioId, UserId = UserId });
            _transactionRepoMock.Setup(r => r.GetBondHoldingsBeforeDateAsync(UserId, payment.AssetId, payment.PaymentDate)).ReturnsAsync(new[] { holding });
            _bondCollectionRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new[]
            {
                new BondCollection { BondPaymentId = payment.Id, AccountId = holding.AccountId, PortfolioId = holding.PortfolioId, Status = BondCollectionStatus.Untracked }
            });

            var dto = new BondCollectionRegisterDTO { BondPaymentId = payment.Id, AccountId = holding.AccountId, PortfolioId = holding.PortfolioId, CapitalAmount = 10m };

            var act = () => _sut.RegisterAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task RegisterAsync_WhenInterestClassDoesNotCountAsIncome_ThrowsBusinessRuleException()
        {
            var payment = MakePayment(id: 10);
            var holding = MakeHolding();
            var badInterestClass = new TransactionClass { Id = 5, UserId = UserId, IncExp = "I", CountsAsIncomeExpense = false };
            _bondPaymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
            _accountRepoMock.Setup(r => r.GetByIdAsync(holding.AccountId)).ReturnsAsync(new Account { Id = holding.AccountId, UserId = UserId });
            _portfolioRepoMock.Setup(r => r.GetByIdAsync(holding.PortfolioId)).ReturnsAsync(new Portfolio { Id = holding.PortfolioId, UserId = UserId });
            _transactionRepoMock.Setup(r => r.GetBondHoldingsBeforeDateAsync(UserId, payment.AssetId, payment.PaymentDate)).ReturnsAsync(new[] { holding });
            _bondCollectionRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(Enumerable.Empty<BondCollection>());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(badInterestClass.Id)).ReturnsAsync(badInterestClass);

            var dto = new BondCollectionRegisterDTO
            {
                BondPaymentId = payment.Id,
                AccountId = holding.AccountId,
                PortfolioId = holding.PortfolioId,
                InterestAmount = 1.33m,
                InterestTransactionClassId = badInterestClass.Id
            };

            var act = () => _sut.RegisterAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // ── DismissAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DismissAsync_CreatesDismissedRecordWithoutTransactions()
        {
            var payment = MakePayment(id: 10);
            var holding = MakeHolding();
            _bondPaymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
            _accountRepoMock.Setup(r => r.GetByIdAsync(holding.AccountId)).ReturnsAsync(new Account { Id = holding.AccountId, UserId = UserId });
            _portfolioRepoMock.Setup(r => r.GetByIdAsync(holding.PortfolioId)).ReturnsAsync(new Portfolio { Id = holding.PortfolioId, UserId = UserId });
            _bondCollectionRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(Enumerable.Empty<BondCollection>());
            _transactionRepoMock.Setup(r => r.GetBondHoldingsBeforeDateAsync(UserId, payment.AssetId, payment.PaymentDate)).ReturnsAsync(new[] { holding });

            var dto = new BondCollectionDismissDTO { BondPaymentId = payment.Id, AccountId = holding.AccountId, PortfolioId = holding.PortfolioId };

            await _sut.DismissAsync(UserId, dto);

            _bondCollectionRepoMock.Verify(r => r.AddAsyncReturnObject(It.Is<BondCollection>(c => c.Status == BondCollectionStatus.Dismissed)), Times.Once);
            _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }

        // ── DeleteAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_RemovesTransactionsAndTheRecord()
        {
            var collection = new BondCollection { Id = 50, UserId = UserId };
            _bondCollectionRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(collection);
            _transactionRepoMock.Setup(r => r.GetByBondCollectionIdAsync(50))
                .ReturnsAsync(new[] { new Transaction { Id = 1 }, new Transaction { Id = 2 } });

            await _sut.DeleteAsync(UserId, 50);

            _transactionRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
            _transactionRepoMock.Verify(r => r.DeleteAsync(2), Times.Once);
            _bondCollectionRepoMock.Verify(r => r.DeleteAsync(50), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotOwner_ThrowsUnauthorized()
        {
            var collection = new BondCollection { Id = 50, UserId = 999 };
            _bondCollectionRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(collection);

            var act = () => _sut.DeleteAsync(UserId, 50);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _bondCollectionRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        // ── GetLastInterestClassIdAsync ────────────────────────────────────────

        [Fact]
        public async Task GetLastInterestClassIdAsync_DelegatesToRepository()
        {
            _transactionRepoMock.Setup(r => r.GetLastBondInterestTransactionClassIdAsync(UserId)).ReturnsAsync(7);

            var result = await _sut.GetLastInterestClassIdAsync(UserId);

            result.Should().Be(7);
        }
    }
}
