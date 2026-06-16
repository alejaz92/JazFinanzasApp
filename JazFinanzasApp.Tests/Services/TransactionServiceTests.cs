using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Transaction;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock;
        private readonly Mock<IInvestmentTransactionRepository> _investmentTransactionRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ISharedExpenseRepository> _sharedExpenseRepoMock;
        private readonly TransactionService _sut;

        private const int UserId = 1;

        public TransactionServiceTests()
        {
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _assetQuoteRepoMock = new Mock<IAssetQuoteRepository>();
            _investmentTransactionRepoMock = new Mock<IInvestmentTransactionRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _sharedExpenseRepoMock = new Mock<ISharedExpenseRepository>();

            _sut = new TransactionService(
                _transactionRepoMock.Object,
                _assetRepoMock.Object,
                _accountRepoMock.Object,
                _transactionClassRepoMock.Object,
                _assetQuoteRepoMock.Object,
                _investmentTransactionRepoMock.Object,
                _portfolioRepoMock.Object,
                _unitOfWorkMock.Object,
                _sharedExpenseRepoMock.Object);
        }

        // ── GetTransactionByIdAsync ───────────────────────────────────────────

        [Fact]
        public async Task GetTransactionByIdAsync_WhenTransactionExists_ReturnsDTO()
        {
            // Arrange
            var transaction = new Transaction
            {
                Id = 10,
                UserId = UserId,
                Date = new DateTime(2026, 1, 15),
                Amount = 100m,
                Detail = "Salario",
                AccountId = 1,
                Account = new Account { Id = 1, Name = "Cuenta ARS" },
                PortfolioId = 1,
                Portfolio = new Portfolio { Id = 1, Name = "Default" },
                AssetId = 1,
                Asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" },
                TransactionClassId = 1,
                TransactionClass = new TransactionClass { Id = 1, Description = "Salario" },
                MovementType = "I"
            };

            _transactionRepoMock
                .Setup(r => r.GetTransactionByIdAsync(10))
                .ReturnsAsync(transaction);

            // Act
            var result = await _sut.GetTransactionByIdAsync(UserId, 10);

            // Assert
            result.Id.Should().Be(10);
            result.AccountName.Should().Be("Cuenta ARS");
            result.AssetSymbol.Should().Be("ARS");
            result.MovementType.Should().Be("I");
        }

        [Fact]
        public async Task GetTransactionByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _transactionRepoMock
                .Setup(r => r.GetTransactionByIdAsync(99))
                .ReturnsAsync((Transaction?)null);

            // Act
            var act = () => _sut.GetTransactionByIdAsync(UserId, 99);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetTransactionByIdAsync_WhenBelongsToOtherUser_ThrowsUnauthorized()
        {
            // Arrange
            var transaction = new Transaction
            {
                Id = 10,
                UserId = 999,
                Account = new Account { Name = "Cuenta ARS" },
                Portfolio = new Portfolio { Name = "Default" },
                Asset = new Asset { Name = "ARS", Symbol = "ARS" },
                TransactionClass = new TransactionClass { Description = "Salario" }
            };

            _transactionRepoMock
                .Setup(r => r.GetTransactionByIdAsync(10))
                .ReturnsAsync(transaction);

            // Act
            var act = () => _sut.GetTransactionByIdAsync(UserId, 10);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── CreateTransactionAsync ────────────────────────────────────────────

        [Fact]
        public async Task CreateTransactionAsync_IncomeWithValidData_AddsTransaction()
        {
            // Arrange
            var dto = new TransactionAddDTO
            {
                movementType = "I",
                assetId = 1,
                incomeAccountId = 2,
                transactionClassId = 3,
                date = new DateTime(2026, 1, 15),
                amount = 500m,
                quotePrice = 1m
            };

            var defaultPortfolio = new Portfolio { Id = 1, Name = "Default", IsDefault = true, UserId = UserId };
            var asset = new Asset { Id = 1, Name = "Dolar Estadounidense", Symbol = "USD" };
            var account = new Account { Id = 2, UserId = UserId, Name = "Cuenta USD" };
            var transactionClass = new TransactionClass { Id = 3, UserId = UserId, IncExp = "I", Description = "Salario" };

            _portfolioRepoMock.Setup(r => r.GetDefaultPortfolio(UserId)).ReturnsAsync(defaultPortfolio);
            _assetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
            _accountRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(account);
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(transactionClass);
            _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            // Act
            await _sut.CreateTransactionAsync(UserId, dto);

            // Assert
            _transactionRepoMock.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
                t.UserId == UserId &&
                t.MovementType == "I" &&
                t.Amount == 500m &&
                t.AccountId == 2)), Times.Once);
        }

        [Fact]
        public async Task CreateTransactionAsync_ExpenseWithInsufficientBalance_ThrowsBusinessRuleException()
        {
            // Arrange
            var dto = new TransactionAddDTO
            {
                movementType = "E",
                assetId = 1,
                expenseAccountId = 2,
                transactionClassId = 3,
                date = new DateTime(2026, 1, 15),
                amount = 1000m,
                quotePrice = 1m
            };

            var defaultPortfolio = new Portfolio { Id = 1, Name = "Default", IsDefault = true, UserId = UserId };
            var asset = new Asset { Id = 1, Name = "Dolar Estadounidense", Symbol = "USD" };
            var account = new Account { Id = 2, UserId = UserId, Name = "Cuenta USD" };
            var transactionClass = new TransactionClass { Id = 3, UserId = UserId, IncExp = "E", Description = "Gastos" };

            _portfolioRepoMock.Setup(r => r.GetDefaultPortfolio(UserId)).ReturnsAsync(defaultPortfolio);
            _assetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
            _accountRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(account);
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(transactionClass);
            _transactionRepoMock.Setup(r => r.GetBalance(2, 1, 1)).ReturnsAsync(100m); // balance menor al monto

            // Act
            var act = () => _sut.CreateTransactionAsync(UserId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*saldo*");
        }

        [Fact]
        public async Task CreateTransactionAsync_WhenDefaultPortfolioMissing_ThrowsNotFoundException()
        {
            // Arrange
            var dto = new TransactionAddDTO { movementType = "I", assetId = 1, incomeAccountId = 2, transactionClassId = 3, amount = 100m };

            _portfolioRepoMock.Setup(r => r.GetDefaultPortfolio(UserId)).ReturnsAsync((Portfolio?)null);

            // Act
            var act = () => _sut.CreateTransactionAsync(UserId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*portfolio*");
        }

        // ── DeleteTransactionAsync ────────────────────────────────────────────

        [Fact]
        public async Task DeleteTransactionAsync_WhenOwner_DeletesTransaction()
        {
            // Arrange
            var transaction = new Transaction { Id = 5, UserId = UserId };
            _transactionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(transaction);
            _transactionRepoMock.Setup(r => r.DeleteAsync(5)).Returns(Task.CompletedTask);

            // Act
            await _sut.DeleteTransactionAsync(UserId, 5);

            // Assert
            _transactionRepoMock.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task DeleteTransactionAsync_WhenNotOwner_ThrowsUnauthorized()
        {
            // Arrange
            var transaction = new Transaction { Id = 5, UserId = 999 };
            _transactionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(transaction);

            // Act
            var act = () => _sut.DeleteTransactionAsync(UserId, 5);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── GetPaginatedTransactionsAsync ─────────────────────────────────────

        [Fact]
        public async Task GetPaginatedTransactionsAsync_ReturnsMappedDTOs()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    Id = 1,
                    UserId = UserId,
                    Date = DateTime.Today,
                    Amount = 200m,
                    Detail = "Compra",
                    AccountId = 1,
                    Account = new Account { Id = 1, Name = "Caja" },
                    PortfolioId = 1,
                    Portfolio = new Portfolio { Id = 1, Name = "Default" },
                    AssetId = 1,
                    Asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" },
                    TransactionClassId = 1,
                    TransactionClass = new TransactionClass { Id = 1, Description = "Gastos Varios" },
                    MovementType = "E"
                }
            };

            _transactionRepoMock
                .Setup(r => r.GetPaginatedTransactions(UserId, 1, 10))
                .ReturnsAsync((transactions.AsEnumerable(), 1));

            // Act
            var (result, totalCount) = await _sut.GetPaginatedTransactionsAsync(UserId, 1, 10);

            // Assert
            totalCount.Should().Be(1);
            result.Should().HaveCount(1);
            result.First().AccountName.Should().Be("Caja");
            result.First().AssetSymbol.Should().Be("ARS");
        }
    }
}
