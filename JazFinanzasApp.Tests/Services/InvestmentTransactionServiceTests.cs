using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class InvestmentTransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IInvestmentTransactionRepository> _investmentTransactionRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly InvestmentTransactionService _sut;

        private const int UserId = 1;

        public InvestmentTransactionServiceTests()
        {
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _investmentTransactionRepoMock = new Mock<IInvestmentTransactionRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _assetQuoteRepoMock = new Mock<IAssetQuoteRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _sut = new InvestmentTransactionService(
                _transactionRepoMock.Object,
                _investmentTransactionRepoMock.Object,
                _assetRepoMock.Object,
                _accountRepoMock.Object,
                _assetQuoteRepoMock.Object,
                _transactionClassRepoMock.Object,
                _portfolioRepoMock.Object,
                _unitOfWorkMock.Object);
        }

        // ── CreateStockTransactionAsync ───────────────────────────────────────

        [Fact]
        public async Task CreateStockTransactionAsync_WhenEnvironmentIsNotStock_ThrowsBusinessRuleException()
        {
            // Arrange
            var dto = new StockTransactionAddDTO { Environment = "Crypto", StockMovementType = "I" };

            // Act
            var act = () => _sut.CreateStockTransactionAsync(UserId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*environment*");
        }

        [Fact]
        public async Task CreateStockTransactionAsync_IncomeMovement_CreatesTransactionAndInvestmentTransaction()
        {
            // Arrange
            var dto = new StockTransactionAddDTO
            {
                Date = new DateTime(2026, 3, 1),
                Environment = "Stock",
                StockMovementType = "I",
                CommerceType = "Transferencia",
                IncomeAssetId = 10,
                IncomeAccountId = 5,
                IncomePortfolioID = 1,
                IncomeQuantity = 100m,
                IncomeQuotePrice = 2m
            };

            var incomeAsset = new Asset { Id = 10, Name = "YPF", Symbol = "YPFD", AssetTypeId = 2, AssetType = new AssetType { Id = 2, Name = "Bono" } };
            var incomeAccount = new Account { Id = 5, UserId = UserId, Name = "Broker" };
            var incomePortfolio = new Portfolio { Id = 1, Name = "Default", UserId = UserId };
            var savedTransaction = new Transaction { Id = 50 };

            _assetRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(incomeAsset);
            _accountRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(incomeAccount);
            _portfolioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(incomePortfolio);
            _transactionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Transaction>())).ReturnsAsync(savedTransaction);
            _investmentTransactionRepoMock.Setup(r => r.AddAsync(It.IsAny<InvestmentTransaction>())).Returns(Task.CompletedTask);

            // Act
            await _sut.CreateStockTransactionAsync(UserId, dto);

            // Assert
            _transactionRepoMock.Verify(r => r.AddAsyncReturnObject(It.Is<Transaction>(t =>
                t.UserId == UserId &&
                t.MovementType == "I" &&
                t.Amount == 100m)), Times.Once);

            _investmentTransactionRepoMock.Verify(r => r.AddAsync(It.Is<InvestmentTransaction>(it =>
                it.UserId == UserId &&
                it.Environment == "Stock" &&
                it.MovementType == "I")), Times.Once);
        }

        // ── CreateCryptoTransactionAsync ──────────────────────────────────────

        [Fact]
        public async Task CreateCryptoTransactionAsync_WhenEnvironmentIsNotCrypto_ThrowsBusinessRuleException()
        {
            // Arrange
            var dto = new InvestmentTransactionAddDTO { Environment = "Stock", MovementType = "I" };

            // Act
            var act = () => _sut.CreateCryptoTransactionAsync(UserId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*environment*");
        }

        // ── DeleteStockTransactionAsync ───────────────────────────────────────

        [Fact]
        public async Task DeleteStockTransactionAsync_WhenOwner_DeletesAllRelatedTransactions()
        {
            // Arrange
            var incomeTransaction = new Transaction { Id = 20, UserId = UserId };
            var expenseTransaction = new Transaction { Id = 21, UserId = UserId };
            var investmentTransaction = new InvestmentTransaction
            {
                Id = 5,
                UserId = UserId,
                IncomeTransaction = incomeTransaction,
                ExpenseTransaction = expenseTransaction
            };

            _investmentTransactionRepoMock.Setup(r => r.GetInvestmentTransactionById(5)).ReturnsAsync(investmentTransaction);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _transactionRepoMock.Setup(r => r.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            _investmentTransactionRepoMock.Setup(r => r.DeleteAsync(5)).Returns(Task.CompletedTask);

            // Act
            await _sut.DeleteStockTransactionAsync(UserId, 5);

            // Assert
            _transactionRepoMock.Verify(r => r.DeleteAsync(20), Times.Once);
            _transactionRepoMock.Verify(r => r.DeleteAsync(21), Times.Once);
            _investmentTransactionRepoMock.Verify(r => r.DeleteAsync(5), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteStockTransactionAsync_WhenNotOwner_ThrowsUnauthorized()
        {
            // Arrange
            var investmentTransaction = new InvestmentTransaction { Id = 5, UserId = 999 };
            _investmentTransactionRepoMock.Setup(r => r.GetInvestmentTransactionById(5)).ReturnsAsync(investmentTransaction);

            // Act
            var act = () => _sut.DeleteStockTransactionAsync(UserId, 5);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task DeleteStockTransactionAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _investmentTransactionRepoMock.Setup(r => r.GetInvestmentTransactionById(99)).ReturnsAsync((InvestmentTransaction?)null);

            // Act
            var act = () => _sut.DeleteStockTransactionAsync(UserId, 99);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── DeleteCryptoTransactionAsync ──────────────────────────────────────

        [Fact]
        public async Task DeleteCryptoTransactionAsync_WhenNotOwner_ThrowsUnauthorized()
        {
            // Arrange
            var investmentTransaction = new InvestmentTransaction { Id = 8, UserId = 999, Environment = "Crypto" };
            _investmentTransactionRepoMock.Setup(r => r.GetInvestmentTransactionById(8)).ReturnsAsync(investmentTransaction);

            // Act
            var act = () => _sut.DeleteCryptoTransactionAsync(UserId, 8);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }
    }
}
