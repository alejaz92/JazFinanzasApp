using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.SharedExpense;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class SharedExpenseServiceTests
    {
        private readonly Mock<ISharedExpenseRepository> _sharedExpenseRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IPersonRepository> _personRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly SharedExpenseService _sut;

        private const int UserId = 1;

        public SharedExpenseServiceTests()
        {
            _sharedExpenseRepoMock = new Mock<ISharedExpenseRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _personRepoMock = new Mock<IPersonRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();

            _sut = new SharedExpenseService(
                _sharedExpenseRepoMock.Object,
                _transactionRepoMock.Object,
                _personRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _accountRepoMock.Object,
                _transactionClassRepoMock.Object,
                _portfolioRepoMock.Object);
        }

        // ── CreateAsync (cuenta) ──────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ForAccountTransaction_CreatesSharedExpenseWithPersonSplit()
        {
            var transaction = new Transaction { Id = 10, UserId = UserId, MovementType = "E", Amount = -1000m };
            var person = new Person { Id = 8, UserId = UserId, Name = "Juan" };
            var dto = new SharedExpenseAddDTO
            {
                TransactionId = 10,
                Splits = new List<SplitInputDTO> { new() { PersonId = 8, Amount = 300m } }
            };

            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);
            _sharedExpenseRepoMock.SetupSequence(r => r.GetByTransactionIdAsync(10))
                .ReturnsAsync((SharedExpense?)null)
                .ReturnsAsync(new SharedExpense { Id = 1, TransactionId = 10, UserId = UserId, Splits = new List<SharedExpenseSplit>() });
            _personRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(person);

            SharedExpense? captured = null;
            _sharedExpenseRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<SharedExpense>()))
                .Callback<SharedExpense>(se => captured = se)
                .ReturnsAsync((SharedExpense se) => se);

            await _sut.CreateAsync(UserId, dto);

            captured.Should().NotBeNull();
            captured!.Splits.Should().ContainSingle();
            captured.Splits.First().PersonId.Should().Be(8);
            captured.Splits.First().Amount.Should().Be(300m);
        }

        [Fact]
        public async Task CreateAsync_WithBothTransactionAndCardTransactionId_ThrowsBusinessRuleException()
        {
            var dto = new SharedExpenseAddDTO
            {
                TransactionId = 10,
                CardTransactionId = 20,
                Splits = new List<SplitInputDTO> { new() { PersonId = 8, Amount = 300m } }
            };

            await FluentActions.Invoking(() => _sut.CreateAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        // ── CreateAsync (tarjeta) ─────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ForCardTransaction_ComputesInstallmentSplitAmount()
        {
            var cardTransaction = new CardTransaction { Id = 20, UserId = UserId, TotalAmount = 1200m, Installments = 6 };
            var person = new Person { Id = 8, UserId = UserId, Name = "Juan" };
            var dto = new SharedExpenseAddDTO
            {
                CardTransactionId = 20,
                Splits = new List<SplitInputDTO> { new() { PersonId = 8, Amount = 300m } }
            };

            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);
            _sharedExpenseRepoMock.SetupSequence(r => r.GetByCardTransactionIdAsync(20))
                .ReturnsAsync((SharedExpense?)null)
                .ReturnsAsync(new SharedExpense { Id = 1, CardTransactionId = 20, UserId = UserId, Splits = new List<SharedExpenseSplit>() });
            _personRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(person);

            SharedExpense? captured = null;
            _sharedExpenseRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<SharedExpense>()))
                .Callback<SharedExpense>(se => captured = se)
                .ReturnsAsync((SharedExpense se) => se);

            await _sut.CreateAsync(UserId, dto);

            captured!.Splits.First().InstallmentSplitAmount.Should().Be(50m); // 300 / 6
        }

        // ── RegisterReimbursementAsync ────────────────────────────────────────

        [Fact]
        public async Task RegisterReimbursementAsync_WithValidData_AddsReimbursementAndUpdatesSplit()
        {
            var sharedExpense = new SharedExpense { Id = 1, CardTransactionId = 20, UserId = UserId };
            var split = new SharedExpenseSplit
            {
                Id = 5,
                SharedExpenseId = 1,
                SharedExpense = sharedExpense,
                PersonId = 8,
                Amount = 300m,
                AmountReimbursed = 0,
                Status = SharedExpenseSplitStatus.Pending
            };
            var account = new Account { Id = 2, UserId = UserId };
            var transactionClass = new TransactionClass { Id = 3, UserId = UserId, Description = "Reintegro", IncExp = "I", IsSystem = true };
            var cardTransaction = new CardTransaction { Id = 20, UserId = UserId, AssetId = 1, Detail = "Compra" };
            var portfolio = new Portfolio { Id = 1, UserId = UserId, IsDefault = true };

            var dto = new RegisterReimbursementDTO
            {
                SplitId = 5,
                AccountId = 2,
                Amount = 300m,
                Date = new DateTime(2026, 1, 1)
            };

            _sharedExpenseRepoMock.Setup(r => r.GetSplitByIdAsync(5)).ReturnsAsync(split);
            _accountRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(account);
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Reintegro", UserId)).ReturnsAsync(transactionClass);
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);
            _portfolioRepoMock.Setup(r => r.GetDefaultPortfolio(UserId)).ReturnsAsync(portfolio);
            _transactionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 99; return t; });

            var result = await _sut.RegisterReimbursementAsync(UserId, dto);

            result.AmountReimbursed.Should().Be(300m);
            result.Status.Should().Be(SharedExpenseSplitStatus.Paid);
            _sharedExpenseRepoMock.Verify(r => r.AddReimbursementAsync(It.Is<SharedExpenseReimbursement>(
                rb => rb.SharedExpenseSplitId == 5 && rb.TransactionId == 99 && rb.Amount == 300m)), Times.Once);
        }

        // ── GetSummaryAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetSummaryAsync_GroupsPendingSplitsByPerson()
        {
            var person = new Person { Id = 8, UserId = UserId, Name = "Juan" };
            var splits = new List<SharedExpenseSplit>
            {
                new()
                {
                    Id = 1,
                    PersonId = 8,
                    Person = person,
                    Amount = 300m,
                    AmountReimbursed = 100m,
                    Status = SharedExpenseSplitStatus.PartiallyPaid,
                    SharedExpense = new SharedExpense { TransactionId = 10, Transaction = new Transaction { Detail = "Super" } }
                }
            };

            _sharedExpenseRepoMock.Setup(r => r.GetPendingSplitsByUserIdAsync(UserId)).ReturnsAsync(splits);

            var result = await _sut.GetSummaryAsync(UserId);

            result.Should().ContainSingle();
            var summary = result.First();
            summary.PersonId.Should().Be(8);
            summary.TotalPending.Should().Be(200m);
        }
    }
}
