using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class CardTransactionServiceTests
    {
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<ICardRepository> _cardRepoMock;
        private readonly Mock<IAsset_UserRepository> _assetUserRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly Mock<IAssetQuoteRepository> _assetQuoteRepoMock;
        private readonly Mock<ICardPaymentRepository> _cardPaymentRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<IAccount_AssetTypeRepository> _accountAssetTypeRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly CardTransactionService _sut;

        private const int UserId = 1;

        public CardTransactionServiceTests()
        {
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _cardRepoMock = new Mock<ICardRepository>();
            _assetUserRepoMock = new Mock<IAsset_UserRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _assetQuoteRepoMock = new Mock<IAssetQuoteRepository>();
            _cardPaymentRepoMock = new Mock<ICardPaymentRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _accountAssetTypeRepoMock = new Mock<IAccount_AssetTypeRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _sut = new CardTransactionService(
                _cardTransactionRepoMock.Object,
                _cardRepoMock.Object,
                _assetUserRepoMock.Object,
                _transactionClassRepoMock.Object,
                _assetRepoMock.Object,
                _assetQuoteRepoMock.Object,
                _cardPaymentRepoMock.Object,
                _accountRepoMock.Object,
                _accountAssetTypeRepoMock.Object,
                _transactionRepoMock.Object,
                _portfolioRepoMock.Object,
                _unitOfWorkMock.Object);
        }

        // ── AddCardTransactionAsync ───────────────────────────────────────────

        [Fact]
        public async Task AddCardTransactionAsync_WithValidData_AddsTransaction()
        {
            // Arrange
            var dto = new CardTransactionAddDTO
            {
                Date = new DateTime(2026, 1, 10),
                Detail = "Supermercado",
                CardId = 1,
                TransactionClassId = 2,
                AssetId = 3,
                TotalAmount = 6000m,
                Installments = 3,
                FirstInstallment = new DateTime(2026, 2, 15),
                LastInstallment = new DateTime(2026, 4, 15),
                Repeat = "NO"
            };

            var card = new Card { Id = 1, UserId = UserId, Name = "Visa" };
            var asset = new Asset { Id = 3, Name = "Peso Argentino", Symbol = "ARS" };
            var assetUser = new Asset_User { UserId = UserId, AssetId = 3 };
            var transactionClass = new TransactionClass { Id = 2, UserId = UserId, Description = "Supermercado" };

            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(card);
            _assetRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(asset);
            _assetUserRepoMock.Setup(r => r.GetUserAssetAsync(UserId, 3)).ReturnsAsync(assetUser);
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(transactionClass);
            _cardTransactionRepoMock.Setup(r => r.AddAsync(It.IsAny<CardTransaction>())).Returns(Task.CompletedTask);

            // Act
            await _sut.AddCardTransactionAsync(UserId, dto);

            // Assert
            _cardTransactionRepoMock.Verify(r => r.AddAsync(It.Is<CardTransaction>(ct =>
                ct.UserId == UserId &&
                ct.CardId == 1 &&
                ct.TotalAmount == 6000m &&
                ct.InstallmentAmount == 2000m &&
                ct.Installments == 3)), Times.Once);
        }

        [Fact]
        public async Task AddCardTransactionAsync_WhenCardNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var dto = new CardTransactionAddDTO { CardId = 99, AssetId = 1, TransactionClassId = 1, Installments = 1, TotalAmount = 100m };

            _cardRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Card?)null);

            // Act
            var act = () => _sut.AddCardTransactionAsync(UserId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Card*");
        }

        [Fact]
        public async Task AddCardTransactionAsync_WhenAssetNotAssignedToUser_ThrowsUnauthorized()
        {
            // Arrange
            var dto = new CardTransactionAddDTO { CardId = 1, AssetId = 5, TransactionClassId = 1, Installments = 1, TotalAmount = 100m };

            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Card { Id = 1, UserId = UserId });
            _assetRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Asset { Id = 5, Name = "BTC", Symbol = "BTC" });
            _assetUserRepoMock.Setup(r => r.GetUserAssetAsync(UserId, 5)).ReturnsAsync((Asset_User?)null);

            // Act
            var act = () => _sut.AddCardTransactionAsync(UserId, dto);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── DeleteCardTransactionAsync ────────────────────────────────────────

        [Fact]
        public async Task DeleteCardTransactionAsync_WhenOwner_DeletesTransaction()
        {
            // Arrange
            var cardTransaction = new CardTransaction { Id = 7, UserId = UserId };
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(cardTransaction);
            _cardTransactionRepoMock.Setup(r => r.DeleteAsync(7)).Returns(Task.CompletedTask);

            // Act
            await _sut.DeleteCardTransactionAsync(UserId, 7);

            // Assert
            _cardTransactionRepoMock.Verify(r => r.DeleteAsync(7), Times.Once);
        }

        [Fact]
        public async Task DeleteCardTransactionAsync_WhenNotOwner_ThrowsUnauthorized()
        {
            // Arrange
            var cardTransaction = new CardTransaction { Id = 7, UserId = 999 };
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(cardTransaction);

            // Act
            var act = () => _sut.DeleteCardTransactionAsync(UserId, 7);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task DeleteCardTransactionAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CardTransaction?)null);

            // Act
            var act = () => _sut.DeleteCardTransactionAsync(UserId, 99);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── GetRecurrentTransactionAsync ──────────────────────────────────────

        [Fact]
        public async Task GetRecurrentTransactionAsync_WhenNonRecurrent_ThrowsBusinessRuleException()
        {
            // Arrange
            var cardTransaction = new CardTransaction { Id = 3, UserId = UserId, Repeat = "NO" };
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(cardTransaction);

            // Act
            var act = () => _sut.GetRecurrentTransactionAsync(UserId, 3);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*recurrent*");
        }
    }
}
