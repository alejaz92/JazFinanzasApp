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
        private readonly Mock<ISharedExpenseRepository> _sharedExpenseRepoMock;
        private readonly Mock<ICardTransactionDiscountRepository> _cardTransactionDiscountRepoMock;
        private readonly Mock<ITripRepository> _tripRepoMock;
        private readonly Mock<ITripSuggestionDismissalRepository> _tripSuggestionDismissalRepoMock;
        private readonly Mock<ISharedEventMovementRepository> _sharedEventMovementRepoMock;
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
            _sharedExpenseRepoMock = new Mock<ISharedExpenseRepository>();
            _cardTransactionDiscountRepoMock = new Mock<ICardTransactionDiscountRepository>();
            _tripRepoMock = new Mock<ITripRepository>();
            _tripSuggestionDismissalRepoMock = new Mock<ITripSuggestionDismissalRepository>();
            _sharedEventMovementRepoMock = new Mock<ISharedEventMovementRepository>();

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
                _unitOfWorkMock.Object,
                _sharedExpenseRepoMock.Object,
                _cardTransactionDiscountRepoMock.Object,
                _tripRepoMock.Object,
                _tripSuggestionDismissalRepoMock.Object,
                _sharedEventMovementRepoMock.Object);
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
            _cardTransactionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<CardTransaction>()))
                .ReturnsAsync((CardTransaction ct) => { ct.Id = 55; return ct; });

            // Act
            var id = await _sut.AddCardTransactionAsync(UserId, dto);

            // Assert
            id.Should().Be(55);
            _cardTransactionRepoMock.Verify(r => r.AddAsyncReturnObject(It.Is<CardTransaction>(ct =>
                ct.UserId == UserId &&
                ct.CardId == 1 &&
                ct.TotalAmount == 6000m &&
                ct.InstallmentAmount == 2000m &&
                ct.Installments == 3)), Times.Once);
        }

        [Fact]
        public async Task AddCardTransactionAsync_WithTrip_SetsTripId()
        {
            // Arrange
            var dto = new CardTransactionAddDTO
            {
                Date = new DateTime(2026, 1, 10),
                Detail = "Vuelo",
                CardId = 1,
                TransactionClassId = 2,
                AssetId = 3,
                TotalAmount = 6000m,
                Installments = 3,
                FirstInstallment = new DateTime(2026, 2, 15),
                LastInstallment = new DateTime(2026, 4, 15),
                Repeat = "NO",
                TripId = 7
            };

            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Card { Id = 1, UserId = UserId, Name = "Visa" });
            _assetRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Asset { Id = 3, Name = "Peso Argentino", Symbol = "ARS" });
            _assetUserRepoMock.Setup(r => r.GetUserAssetAsync(UserId, 3)).ReturnsAsync(new Asset_User { UserId = UserId, AssetId = 3 });
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new TransactionClass { Id = 2, UserId = UserId, Description = "Vuelos" });
            _tripRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Trip { Id = 7, UserId = UserId, Name = "Bariloche" });
            _cardTransactionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<CardTransaction>()))
                .ReturnsAsync((CardTransaction ct) => ct);

            // Act
            await _sut.AddCardTransactionAsync(UserId, dto);

            // Assert
            _cardTransactionRepoMock.Verify(r => r.AddAsyncReturnObject(It.Is<CardTransaction>(ct => ct.TripId == 7)), Times.Once);
        }

        [Fact]
        public async Task AddCardTransactionAsync_WithTripOfAnotherUser_ThrowsUnauthorized()
        {
            // Arrange
            var dto = new CardTransactionAddDTO
            {
                CardId = 1,
                TransactionClassId = 2,
                AssetId = 3,
                TotalAmount = 6000m,
                Installments = 3,
                Repeat = "NO",
                TripId = 7
            };

            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Card { Id = 1, UserId = UserId, Name = "Visa" });
            _assetRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Asset { Id = 3, Name = "Peso Argentino", Symbol = "ARS" });
            _assetUserRepoMock.Setup(r => r.GetUserAssetAsync(UserId, 3)).ReturnsAsync(new Asset_User { UserId = UserId, AssetId = 3 });
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new TransactionClass { Id = 2, UserId = UserId, Description = "Vuelos" });
            _tripRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Trip { Id = 7, UserId = 999, Name = "Bariloche" });

            // Act
            var act = () => _sut.AddCardTransactionAsync(UserId, dto);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedDomainException>();
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
            _tripSuggestionDismissalRepoMock.Verify(r => r.DeleteByCardTransactionIdAsync(7), Times.Once);
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

        [Fact]
        public async Task DeleteCardTransactionAsync_WhenReferencedBySharedEvent_ThrowsBusinessRuleException()
        {
            // Arrange
            var cardTransaction = new CardTransaction { Id = 7, UserId = UserId };
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(cardTransaction);
            _sharedEventMovementRepoMock.Setup(r => r.IsCardTransactionReferencedAsync(7)).ReturnsAsync(true);

            // Act
            var act = () => _sut.DeleteCardTransactionAsync(UserId, 7);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();
            _cardTransactionRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
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

        // ── RegisterCardPaymentAsync ─────────────────────────────────────────

        private CardTransactionPaymentDTO MakePaymentDto(int installmentNumber = 1, decimal installmentAmount = 200m)
        {
            return new CardTransactionPaymentDTO
            {
                CardId = 1,
                PaymentMonth = new DateTime(2026, 1, 1),
                PaymentDate = new DateTime(2026, 1, 1),
                accountId = 2,
                PaymentAsset = "P",
                PesosAmount = 0,
                DolarAmount = null,
                CardExpenses = 0,
                CardTransactions = new List<CardTransactionPaymentListDTO>
                {
                    new()
                    {
                        CardTransactionId = 20,
                        Date = new DateTime(2026, 1, 1),
                        CardId = 1,
                        TransactionClassId = 3,
                        Detail = "Compra",
                        AssetId = 1,
                        Installment = $"{installmentNumber}/6",
                        InstallmentNumber = installmentNumber,
                        InstallmentAmount = installmentAmount,
                        ValueInPesos = installmentAmount
                    }
                }
            };
        }

        private void SetupRegisterCardPaymentHappyPathDependencies()
        {
            var card = new Card { Id = 1, UserId = UserId, Name = "Visa" };
            var account = new Account { Id = 2, UserId = UserId };
            var peso = new Asset { Id = 1, Name = "Peso Argentino" };
            var dolar = new Asset { Id = 2, Name = "Dolar Estadounidense" };
            var portfolio = new Portfolio { Id = 1, UserId = UserId, IsDefault = true };
            var gastosTarjetaClass = new TransactionClass { Id = 4, UserId = UserId, Description = "Gastos Tarjeta" };
            var cardTransactionClass = new TransactionClass { Id = 3, UserId = UserId, Description = "Compras" };

            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(card);
            _accountRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(account);
            _accountAssetTypeRepoMock.Setup(r => r.GetAccount_AssetTypeByAccountIdAndAssetTypeNameAsync(2, "Moneda"))
                .ReturnsAsync(new Account_AssetType { AccountId = 2, AssetTypeId = 1 });
            _assetRepoMock.Setup(r => r.GetAssetByNameAsync("Peso Argentino")).ReturnsAsync(peso);
            _assetRepoMock.Setup(r => r.GetAssetByNameAsync("Dolar Estadounidense")).ReturnsAsync(dolar);
            _assetQuoteRepoMock.Setup(r => r.GetQuotePrice(peso.Id, It.IsAny<DateTime>(), "BLUE")).ReturnsAsync(1m);
            _portfolioRepoMock.Setup(r => r.GetDefaultPortfolio(UserId)).ReturnsAsync(portfolio);
            _transactionRepoMock.Setup(r => r.GetBalance(account.Id, peso.Id, portfolio.Id)).ReturnsAsync(100000m);
            _assetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(peso);
            _assetUserRepoMock.Setup(r => r.GetUserAssetAsync(UserId, 1)).ReturnsAsync(new Asset_User { UserId = UserId, AssetId = 1 });
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(cardTransactionClass);
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Gastos Tarjeta", UserId)).ReturnsAsync(gastosTarjetaClass);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithDiscountInstallment_AppliesDiscountAndDeletesInstallment()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);

            var discount = new CardTransactionDiscount { Id = 1, CardTransactionId = 20, AmountApplied = 0 };
            var installments = new List<CardTransactionDiscountInstallment>
            {
                new() { Id = 10, CardTransactionDiscountId = 1, TransactionId = 500, Amount = 200m, InstallmentNumber = 1 }
            };
            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync(discount);
            _cardTransactionDiscountRepoMock.Setup(r => r.GetInstallmentsByDiscountIdAsync(1)).ReturnsAsync(installments);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((SharedExpense?)null);

            Transaction? capturedExpenseTransaction = null;
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => { if (t.Detail.Contains("Compra")) capturedExpenseTransaction = t; })
                .Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            capturedExpenseTransaction!.Amount.Should().Be(0m); // -200 (cuota) + 200 (descuento)
            _cardTransactionDiscountRepoMock.Verify(r => r.DeleteInstallmentAsync(10), Times.Once);
            _transactionRepoMock.Verify(r => r.DeleteAsync(500), Times.Once);
            _cardTransactionDiscountRepoMock.Verify(r => r.UpdateAsync(It.Is<CardTransactionDiscount>(d => d.AmountApplied == 200m)), Times.Once);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithPersonPoolReimbursement_AppliesPersonPoolWithoutTouchingDiscount()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);

            var sharedExpense = new SharedExpense { Id = 1, CardTransactionId = 20, UserId = UserId };
            var split = new SharedExpenseSplit
            {
                Id = 5,
                SharedExpenseId = 1,
                SharedExpense = sharedExpense,
                PersonId = 8,
                Amount = 300m,
                AmountReimbursed = 300m,
                AmountApplied = 0,
                InstallmentSplitAmount = 50m
            };
            sharedExpense.Splits = new List<SharedExpenseSplit> { split };

            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync(sharedExpense);
            _sharedExpenseRepoMock.Setup(r => r.GetReimbursementsBySplitIdAsync(5)).ReturnsAsync(new List<SharedExpenseReimbursement>());

            Transaction? capturedExpenseTransaction = null;
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => { if (t.Detail.Contains("Compra")) capturedExpenseTransaction = t; })
                .Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            capturedExpenseTransaction!.Amount.Should().Be(-150m); // -200 (cuota) + 50 (pool de persona)
            _sharedExpenseRepoMock.Verify(r => r.UpdateSplitAsync(It.Is<SharedExpenseSplit>(s => s.AmountApplied == 50m)), Times.Once);
            _cardTransactionDiscountRepoMock.Verify(r => r.UpdateAsync(It.IsAny<CardTransactionDiscount>()), Times.Never);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_LinksInstallmentTransactionToCardTransaction()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);

            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((SharedExpense?)null);

            var capturedTransactions = new List<Transaction>();
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => capturedTransactions.Add(t))
                .Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            var installmentTransaction = capturedTransactions.Single(t => t.Detail!.Contains("Compra"));
            installmentTransaction.CardTransactionId.Should().Be(20);

            var cardExpensesTransaction = capturedTransactions.Single(t => t.Detail!.Contains("Gastos Tarjeta"));
            cardExpensesTransaction.CardTransactionId.Should().BeNull();
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithPersonAndDiscountTogether_AppliesBothIndependently()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);

            var discount = new CardTransactionDiscount { Id = 1, CardTransactionId = 20, AmountApplied = 0 };
            var discountInstallments = new List<CardTransactionDiscountInstallment>
            {
                new() { Id = 10, CardTransactionDiscountId = 1, TransactionId = 500, Amount = 200m, InstallmentNumber = 1 }
            };
            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync(discount);
            _cardTransactionDiscountRepoMock.Setup(r => r.GetInstallmentsByDiscountIdAsync(1)).ReturnsAsync(discountInstallments);

            var sharedExpense = new SharedExpense { Id = 1, CardTransactionId = 20, UserId = UserId };
            var split = new SharedExpenseSplit
            {
                Id = 5,
                SharedExpenseId = 1,
                SharedExpense = sharedExpense,
                PersonId = 8,
                Amount = 300m,
                AmountReimbursed = 300m,
                AmountApplied = 0,
                InstallmentSplitAmount = 50m
            };
            sharedExpense.Splits = new List<SharedExpenseSplit> { split };
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync(sharedExpense);
            _sharedExpenseRepoMock.Setup(r => r.GetReimbursementsBySplitIdAsync(5)).ReturnsAsync(new List<SharedExpenseReimbursement>());

            Transaction? capturedExpenseTransaction = null;
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => { if (t.Detail.Contains("Compra")) capturedExpenseTransaction = t; })
                .Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            capturedExpenseTransaction!.Amount.Should().Be(50m); // -200 (cuota) + 200 (descuento) + 50 (pool de persona)
            _cardTransactionDiscountRepoMock.Verify(r => r.UpdateAsync(It.Is<CardTransactionDiscount>(d => d.AmountApplied == 200m)), Times.Once);
            _sharedExpenseRepoMock.Verify(r => r.UpdateSplitAsync(It.Is<SharedExpenseSplit>(s => s.AmountApplied == 50m)), Times.Once);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithManualEntry_StoresNullCardTransactionIdInsteadOfZero()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);
            dto.CardTransactions.Add(new CardTransactionPaymentListDTO
            {
                CardTransactionId = 0, // fila manual agregada a mano en el formulario de pago, sin CardTransaction real
                Date = new DateTime(2026, 1, 1),
                CardId = 1,
                TransactionClassId = 3,
                Detail = "Gasto manual",
                AssetId = 1,
                Installment = "1/1",
                InstallmentNumber = 1,
                InstallmentAmount = 50m,
                ValueInPesos = 50m
            });

            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((SharedExpense?)null);
            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(0)).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(0)).ReturnsAsync((SharedExpense?)null);

            var capturedTransactions = new List<Transaction>();
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => capturedTransactions.Add(t))
                .Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            var manualTransaction = capturedTransactions.Single(t => t.Detail!.Contains("Gasto manual"));
            manualTransaction.CardTransactionId.Should().BeNull();
        }
    }
}
