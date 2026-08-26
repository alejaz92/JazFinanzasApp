using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
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
        private readonly Mock<IQuotePriceResolver> _quotePriceResolverMock;
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
            _quotePriceResolverMock = new Mock<IQuotePriceResolver>();
            _quotePriceResolverMock.Setup(r => r.ResolveAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(1m);

            // Real, no mockeado: el valor de estos tests es ver el circuito completo -- materializar
            // el saldo a favor y despues consumirlo dentro de la cuota -- cerrando en los montos.
            var discountService = new CardTransactionDiscountService(
                _cardTransactionDiscountRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _accountRepoMock.Object,
                _transactionClassRepoMock.Object,
                _transactionRepoMock.Object,
                _portfolioRepoMock.Object,
                _cardPaymentRepoMock.Object,
                _quotePriceResolverMock.Object);

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
                _sharedEventMovementRepoMock.Object,
                discountService);
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

        private (SharedExpense sharedExpense, SharedExpenseSplit split, SharedExpenseReimbursement reimbursement) MakeFullyConsumedReimbursementSetup()
        {
            var sharedExpense = new SharedExpense { Id = 1, CardTransactionId = 20, UserId = UserId };
            var split = new SharedExpenseSplit
            {
                Id = 5,
                SharedExpenseId = 1,
                SharedExpense = sharedExpense,
                PersonId = 8,
                Amount = 50m,
                AmountReimbursed = 50m,
                AmountApplied = 0,
                InstallmentSplitAmount = 50m
            };
            sharedExpense.Splits = new List<SharedExpenseSplit> { split };
            var reimbursement = new SharedExpenseReimbursement { Id = 100, SharedExpenseSplitId = 5, TransactionId = 900, Amount = 50m, Date = new DateTime(2026, 1, 1) };

            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync(sharedExpense);
            _sharedExpenseRepoMock.Setup(r => r.GetReimbursementsBySplitIdAsync(5)).ReturnsAsync(new List<SharedExpenseReimbursement> { reimbursement });

            return (sharedExpense, split, reimbursement);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithFullyConsumedReimbursement_ConsolidatesItIntoTheInstallment()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);
            var (_, _, reimbursement) = MakeFullyConsumedReimbursementSetup();

            Transaction? capturedExpenseTransaction = null;
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => { if (t.Detail!.Contains("Compra")) capturedExpenseTransaction = t; })
                .Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            capturedExpenseTransaction!.Amount.Should().Be(-150m); // -200 (cuota) + 50 (reintegro consolidado)
            _sharedExpenseRepoMock.Verify(r => r.DeleteReimbursementAsync(reimbursement.Id), Times.Once);
            _transactionRepoMock.Verify(r => r.DeleteAsync(reimbursement.TransactionId), Times.Once);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithFullyConsumedReimbursement_DetachesItFromSharedEventPaymentAllocationsBeforeDeleting()
        {
            // El placeholder que crea el motor de pagos de Eventos Compartidos sigue referenciado desde
            // SharedEventPaymentAllocations (FK real). Hay que soltar esa FK antes de borrarlo, o el DELETE
            // tira DbUpdateException y hace fallar todo el pago de tarjeta.
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);
            var (_, _, reimbursement) = MakeFullyConsumedReimbursementSetup();

            var callOrder = new List<string>();
            _transactionRepoMock.Setup(r => r.DetachConsumedIncomeFromSharedEventPaymentAllocationsAsync(reimbursement.TransactionId))
                .Callback(() => callOrder.Add("detach")).Returns(Task.CompletedTask);
            _transactionRepoMock.Setup(r => r.DeleteAsync(reimbursement.TransactionId))
                .Callback(() => callOrder.Add("delete")).Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            callOrder.Should().Equal("detach", "delete");
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

        // ── Filas agregadas a mano al pagar (plan-viajes-historicos.md, Fase 6C) ──

        private CardTransactionPaymentDTO MakeManualEntryPaymentDto(
            DateTime? fecha = null, decimal installmentAmount = 5000m)
        {
            var dto = MakePaymentDto();
            dto.CardTransactions.Add(new CardTransactionPaymentListDTO
            {
                CardTransactionId = 0,                 // así llega una fila agregada a mano
                Date = fecha ?? new DateTime(2025, 12, 20),
                CardId = 1,
                TransactionClassId = 3,
                Detail = "Swiss Medical",
                AssetId = 1,
                Installment = "1/1",
                InstallmentNumber = 0,
                InstallmentAmount = installmentAmount,
                ValueInPesos = installmentAmount
            });
            return dto;
        }

        private (List<Transaction> transactions, List<CardTransaction> cardTransactions) CaptureOnPayment()
        {
            var transactions = new List<Transaction>();
            var cardTransactions = new List<CardTransaction>();
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => transactions.Add(t)).Returns(Task.CompletedTask);
            _cardTransactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<CardTransaction>()))
                .Callback<CardTransaction>(c => cardTransactions.Add(c)).Returns(Task.CompletedTask);
            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(It.IsAny<int>())).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(It.IsAny<int>())).ReturnsAsync((SharedExpense?)null);
            return (transactions, cardTransactions);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_ManualEntry_CreatesItsCardTransaction()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var (_, cardTransactions) = CaptureOnPayment();

            await _sut.RegisterCardPaymentAsync(UserId, MakeManualEntryPaymentDto());

            var creado = cardTransactions.Should().ContainSingle().Subject;
            creado.Detail.Should().Be("Swiss Medical");
            creado.CardId.Should().Be(1);
            creado.UserId.Should().Be(UserId);
            creado.TransactionClassId.Should().Be(3);
            creado.AssetId.Should().Be(1);
            creado.TotalAmount.Should().Be(5000m);
            creado.InstallmentAmount.Should().Be(5000m);
            creado.Installments.Should().Be(1);
            creado.Repeat.Should().Be("NO");
            creado.Date.Should().Be(new DateTime(2025, 12, 20));
            // se devenga en el mes que se está pagando, así que no reaparece el mes siguiente
            creado.FirstInstallment.Should().Be(new DateTime(2026, 1, 1));
            creado.LastInstallment.Should().Be(new DateTime(2026, 1, 1));
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_ManualEntry_LinksItsInstallmentTransactionToTheNewCardTransaction()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var (transactions, cardTransactions) = CaptureOnPayment();

            await _sut.RegisterCardPaymentAsync(UserId, MakeManualEntryPaymentDto());

            var creado = cardTransactions.Single();
            var cuota = transactions.Single(t => t.Detail!.Contains("Swiss Medical"));
            // se vincula por navegación: el consumo todavía no tiene Id asignado
            cuota.CardTransaction.Should().BeSameAs(creado);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_ManualEntryWithoutDate_UsesThePaymentMonth()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var (_, cardTransactions) = CaptureOnPayment();

            await _sut.RegisterCardPaymentAsync(UserId, MakeManualEntryPaymentDto(fecha: default(DateTime)));

            cardTransactions.Single().Date.Should().Be(new DateTime(2026, 1, 1));
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_RegularRows_DoNotCreateAnyCardTransaction()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var (transactions, cardTransactions) = CaptureOnPayment();

            await _sut.RegisterCardPaymentAsync(UserId, MakePaymentDto());

            cardTransactions.Should().BeEmpty();
            transactions.Single(t => t.Detail!.Contains("Compra")).CardTransactionId.Should().Be(20);
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

        [Fact]
        public async Task RegisterCardPaymentAsync_WithNextClosingAndDueDate_UpdatesCard()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);
            dto.NextClosingDate = new DateTime(2026, 2, 20);
            dto.NextDueDate = new DateTime(2026, 2, 27);

            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((SharedExpense?)null);
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            _cardRepoMock.Verify(r => r.UpdateAsync(It.Is<Card>(c =>
                c.NextClosingDate == dto.NextClosingDate && c.NextDueDate == dto.NextDueDate)), Times.Once);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithoutNextClosingAndDueDate_DoesNotUpdateCard()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);

            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync((SharedExpense?)null);
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            _cardRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Card>()), Times.Never);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithNextDueDateBeforeNextClosingDate_ThrowsBusinessRuleException()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 200m);
            dto.NextClosingDate = new DateTime(2026, 2, 27);
            dto.NextDueDate = new DateTime(2026, 2, 20);

            var act = () => _sut.RegisterCardPaymentAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _cardPaymentRepoMock.Verify(r => r.AddAsync(It.IsAny<CardPayment>()), Times.Never);
        }
    
        // -- Saldo a favor de la tarjeta absorbido por el resumen (Fase 4) --

        // Fake chico del almacenamiento de cuotas del descuento: hace falta que lo que MaterializeAsync
        // escribe lo lea despues el consumo, dentro de la misma llamada a RegisterCardPaymentAsync.
        private (List<Transaction> Creados, List<int> Borrados) WireDiscountStorage(CardTransactionDiscount discount)
        {
            var creados = new List<Transaction>();
            var borrados = new List<int>();
            var cuotas = new List<CardTransactionDiscountInstallment>();
            var siguienteId = 1000;

            _transactionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = ++siguienteId; creados.Add(t); return t; });
            _transactionRepoMock.Setup(r => r.DeleteAsync(It.IsAny<int>()))
                .Callback<int>(borrados.Add).Returns(Task.CompletedTask);

            _cardTransactionDiscountRepoMock.Setup(r => r.AddInstallmentAsync(It.IsAny<CardTransactionDiscountInstallment>()))
                .Callback<CardTransactionDiscountInstallment>(i => { i.Id = cuotas.Count + 1; cuotas.Add(i); })
                .Returns(Task.CompletedTask);
            _cardTransactionDiscountRepoMock.Setup(r => r.GetInstallmentsByDiscountIdAsync(discount.Id))
                .ReturnsAsync(() => cuotas.ToList());
            _cardTransactionDiscountRepoMock.Setup(r => r.DeleteInstallmentAsync(It.IsAny<int>()))
                .Callback<int>(id => cuotas.RemoveAll(c => c.Id == id)).Returns(Task.CompletedTask);

            _cardTransactionDiscountRepoMock.Setup(r => r.GetPendingOnCardAsync(1, UserId))
                .ReturnsAsync(() => discount.AmountMaterialized < discount.Amount
                    ? new[] { discount } : Array.Empty<CardTransactionDiscount>());

            return (creados, borrados);
        }

        private CardTransaction MakePromotedPurchase() => new()
        {
            Id = 20,
            UserId = UserId,
            CardId = 1,
            AssetId = 1,
            Detail = "Compra promocionada",
            TotalAmount = 100000m,
            Installments = 5,
            InstallmentAmount = 20000m,
            FirstInstallment = new DateTime(2026, 1, 1)
        };

        private TransactionClass MakeReintegroClass() =>
            new() { Id = 5, UserId = UserId, Description = "Reintegro", IncExp = "I" };

        [Fact]
        public async Task RegisterCardPaymentAsync_WhenStatementAbsorbsCardCredit_LeavesTheAccountMatchingWhatTheBankDebited()
        {
            // Ejemplo maestro del plan, mes 1: compra de 100.000 en 5 cuotas de 20.000 con 35.000 de
            // reintegro sobre la tarjeta, mas otra compra sin promocion de 50.000. El banco aplica los
            // 35.000 contra el total y debita 35.000.
            SetupRegisterCardPaymentHappyPathDependencies();
            var otraCompra = new CardTransaction
            {
                Id = 21,
                UserId = UserId,
                CardId = 1,
                AssetId = 1,
                Detail = "Otra compra",
                TotalAmount = 50000m,
                Installments = 1,
                InstallmentAmount = 50000m,
                FirstInstallment = new DateTime(2026, 1, 1)
            };
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(MakePromotedPurchase());
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(21)).ReturnsAsync(otraCompra);
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Reintegro", UserId))
                .ReturnsAsync(MakeReintegroClass());

            var discount = new CardTransactionDiscount
            {
                Id = 1,
                CardTransactionId = 20,
                UserId = UserId,
                Amount = 35000m,
                AmountApplied = 0m,
                AmountMaterialized = 0m,
                CreditTarget = CardTransactionDiscountCreditTarget.Card,
                CreditDate = new DateTime(2026, 1, 5)
            };
            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync(discount);
            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(21)).ReturnsAsync((CardTransactionDiscount?)null);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(It.IsAny<int>())).ReturnsAsync((SharedExpense?)null);

            var almacen = WireDiscountStorage(discount);
            var ingresosCreados = almacen.Creados;
            var transaccionesBorradas = almacen.Borrados;

            var egresos = new List<Transaction>();
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(egresos.Add).Returns(Task.CompletedTask);

            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 20000m);
            dto.CardCreditApplied = 35000m;
            dto.PesosAmount = 35000m;
            dto.CardTransactions[0].Installment = "1/5";
            dto.CardTransactions[0].Detail = "Compra promocionada";
            dto.CardTransactions.Add(new CardTransactionPaymentListDTO
            {
                CardTransactionId = 21,
                Date = new DateTime(2026, 1, 1),
                CardId = 1,
                TransactionClassId = 3,
                Detail = "Otra compra",
                AssetId = 1,
                Installment = "1/1",
                InstallmentNumber = 1,
                InstallmentAmount = 50000m,
                ValueInPesos = 50000m
            });

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            // La cuota de la compra promocionada queda tapada; la otra compra se paga entera.
            egresos.Single(t => t.Detail.Contains("Compra promocionada")).Amount.Should().Be(0m);
            egresos.Single(t => t.Detail.Contains("Otra compra")).Amount.Should().Be(-50000m);

            // Se materializo todo el credito, repartido en cuota 1 (20.000) y cuota 2 (15.000).
            ingresosCreados.Select(t => t.Amount).Should().BeEquivalentTo(new[] { 20000m, 15000m });
            discount.AmountMaterialized.Should().Be(35000m);

            // El tramo de la cuota 1 se consumio y su ingreso desaparecio; el de la cuota 2 sigue vivo.
            discount.AmountApplied.Should().Be(20000m);
            var ingresoConsumido = ingresosCreados.Single(t => t.Amount == 20000m);
            transaccionesBorradas.Should().ContainSingle().Which.Should().Be(ingresoConsumido.Id);

            // Lo que importa de verdad: lo que sale de la cuenta coincide con lo que debito el banco.
            var ingresosVivos = ingresosCreados.Where(t => !transaccionesBorradas.Contains(t.Id)).Sum(t => t.Amount);
            (egresos.Sum(t => t.Amount) + ingresosVivos).Should().Be(-35000m);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WhenCreditExceedsTheStatement_LeavesTheRestPendingOnTheCard()
        {
            // Variante del plan: el resumen es solo la cuota 1 (20.000), asi que el banco aplica 20.000
            // y quedan 15.000 de saldo a favor en la tarjeta para el mes siguiente.
            SetupRegisterCardPaymentHappyPathDependencies();
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(MakePromotedPurchase());
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Reintegro", UserId))
                .ReturnsAsync(MakeReintegroClass());

            var discount = new CardTransactionDiscount
            {
                Id = 1,
                CardTransactionId = 20,
                UserId = UserId,
                Amount = 35000m,
                AmountApplied = 0m,
                AmountMaterialized = 0m,
                CreditTarget = CardTransactionDiscountCreditTarget.Card,
                CreditDate = new DateTime(2026, 1, 5)
            };
            _cardTransactionDiscountRepoMock.Setup(r => r.GetByCardTransactionIdAsync(20)).ReturnsAsync(discount);
            _sharedExpenseRepoMock.Setup(r => r.GetByCardTransactionIdAsync(It.IsAny<int>())).ReturnsAsync((SharedExpense?)null);

            var almacen = WireDiscountStorage(discount);
            var ingresosCreados = almacen.Creados;
            var transaccionesBorradas = almacen.Borrados;

            var egresos = new List<Transaction>();
            _transactionRepoMock.Setup(r => r.AddAsyncTransaction(It.IsAny<Transaction>()))
                .Callback<Transaction>(egresos.Add).Returns(Task.CompletedTask);

            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 20000m);
            dto.CardCreditApplied = 20000m;
            dto.PesosAmount = 0m;

            await _sut.RegisterCardPaymentAsync(UserId, dto);

            egresos.Single(t => t.Detail.Contains("Compra")).Amount.Should().Be(0m);
            discount.AmountMaterialized.Should().Be(20000m);
            (discount.Amount - discount.AmountMaterialized).Should().Be(15000m);

            var ingresosVivos = ingresosCreados.Where(t => !transaccionesBorradas.Contains(t.Id)).Sum(t => t.Amount);
            (egresos.Sum(t => t.Amount) + ingresosVivos).Should().Be(0m);
        }

        [Fact]
        public async Task RegisterCardPaymentAsync_WithCardCreditAboveWhatIsPending_ThrowsAndRollsBack()
        {
            SetupRegisterCardPaymentHappyPathDependencies();
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(MakePromotedPurchase());

            var discount = new CardTransactionDiscount
            {
                Id = 1,
                CardTransactionId = 20,
                UserId = UserId,
                Amount = 35000m,
                AmountApplied = 0m,
                AmountMaterialized = 30000m,
                CreditTarget = CardTransactionDiscountCreditTarget.Card,
                CreditDate = new DateTime(2026, 1, 5)
            };
            _cardTransactionDiscountRepoMock.Setup(r => r.GetPendingOnCardAsync(1, UserId)).ReturnsAsync(new[] { discount });

            var dto = MakePaymentDto(installmentNumber: 1, installmentAmount: 20000m);
            dto.CardCreditApplied = 10000m;

            await FluentActions.Invoking(() => _sut.RegisterCardPaymentAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();

            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            discount.AmountMaterialized.Should().Be(30000m);
        }
}
}
