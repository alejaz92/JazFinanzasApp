using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class SharedEventServiceTests
    {
        private readonly Mock<ISharedEventRepository> _sharedEventRepoMock;
        private readonly Mock<ISharedEventMovementRepository> _sharedEventMovementRepoMock;
        private readonly Mock<IPersonRepository> _personRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly Mock<IAsset_UserRepository> _assetUserRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<ICardRepository> _cardRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<ICardTransactionService> _cardTransactionServiceMock;
        private readonly Mock<ISharedExpenseRepository> _sharedExpenseRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ISharedEventPaymentRepository> _sharedEventPaymentRepoMock;
        private readonly SharedEventService _sut;

        private const int UserId = 1;
        private const int EventId = 100;

        public SharedEventServiceTests()
        {
            _sharedEventRepoMock = new Mock<ISharedEventRepository>();
            _sharedEventMovementRepoMock = new Mock<ISharedEventMovementRepository>();
            _personRepoMock = new Mock<IPersonRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _assetUserRepoMock = new Mock<IAsset_UserRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _cardRepoMock = new Mock<ICardRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _cardTransactionServiceMock = new Mock<ICardTransactionService>();
            _sharedExpenseRepoMock = new Mock<ISharedExpenseRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _sharedEventPaymentRepoMock = new Mock<ISharedEventPaymentRepository>();

            _sut = new SharedEventService(
                _sharedEventRepoMock.Object,
                _sharedEventMovementRepoMock.Object,
                _personRepoMock.Object,
                _assetRepoMock.Object,
                _assetUserRepoMock.Object,
                _transactionClassRepoMock.Object,
                _accountRepoMock.Object,
                _cardRepoMock.Object,
                _transactionRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _cardTransactionServiceMock.Object,
                _sharedExpenseRepoMock.Object,
                _portfolioRepoMock.Object,
                _unitOfWorkMock.Object,
                _sharedEventPaymentRepoMock.Object);
        }

        private static SharedEvent BuildEvent(params Person[] people)
        {
            return new SharedEvent
            {
                Id = EventId,
                UserId = UserId,
                Name = "Asado",
                IsClosed = false,
                Participants = people.Select(p => new SharedEventParticipant { PersonId = p.Id, Person = p }).ToList(),
                Movements = new List<SharedEventMovement>(),
                Payments = new List<SharedEventPayment>()
            };
        }

        private static readonly Person Juan = new() { Id = 8, UserId = UserId, Name = "Juan" };
        private static readonly Person Pedro = new() { Id = 9, UserId = UserId, Name = "Pedro" };

        // ── CreateMovementAsync ───────────────────────────────────────────────

        [Fact]
        public async Task CreateMovementAsync_PaidByUserWithAccount_CreatesNegativeTransactionAndSplit()
        {
            var sharedEvent = BuildEvent(Juan);
            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var transactionClass = new TransactionClass { Id = 5, UserId = UserId, Description = "Comida", IncExp = "E" };
            var account = new Account { Id = 2, UserId = UserId };
            var portfolio = new Portfolio { Id = 1, UserId = UserId };

            var dto = new SharedEventMovementAddDTO
            {
                Date = new DateTime(2026, 5, 25),
                Description = "Carne y hamburguesas",
                TransactionClassId = 5,
                AssetId = 1,
                TotalAmount = 30000m,
                PayerPersonId = null,
                Shares = new List<SharedEventMovementShareInputDTO>
                {
                    new() { PersonId = null, Amount = 20000m },
                    new() { PersonId = 8, Amount = 10000m }
                },
                Payment = new SharedEventMovementPaymentInputDTO { AccountId = 2 }
            };

            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);
            _assetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
            _assetUserRepoMock.Setup(r => r.GetUserAssetAsync(UserId, 1)).ReturnsAsync(new Asset_User());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(transactionClass);
            _accountRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(account);
            _portfolioRepoMock.Setup(r => r.GetDefaultPortfolio(UserId)).ReturnsAsync(portfolio);
            _transactionRepoMock.Setup(r => r.GetBalance(2, 1, 1)).ReturnsAsync(100000m);

            Transaction? capturedTransaction = null;
            _transactionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => capturedTransaction = t)
                .ReturnsAsync((Transaction t) => { t.Id = 50; return t; });

            SharedExpense? capturedSharedExpense = null;
            _sharedExpenseRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<SharedExpense>()))
                .Callback<SharedExpense>(se => capturedSharedExpense = se)
                .ReturnsAsync((SharedExpense se) =>
                {
                    se.Id = 70;
                    var i = 1;
                    foreach (var split in se.Splits) split.Id = i++;
                    return se;
                });

            SharedEventMovement? capturedMovement = null;
            _sharedEventMovementRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<SharedEventMovement>()))
                .Callback<SharedEventMovement>(m => capturedMovement = m)
                .ReturnsAsync((SharedEventMovement m) => { m.Id = 900; return m; });
            _sharedEventMovementRepoMock.Setup(r => r.GetDetailByIdAsync(900))
                .ReturnsAsync(() => capturedMovement);

            var result = await _sut.CreateMovementAsync(UserId, EventId, dto);

            capturedTransaction.Should().NotBeNull();
            capturedTransaction!.Amount.Should().Be(-30000m);
            capturedTransaction.MovementType.Should().Be("E");

            capturedSharedExpense.Should().NotBeNull();
            capturedSharedExpense!.TransactionId.Should().Be(50);
            capturedSharedExpense.Splits.Should().ContainSingle();
            capturedSharedExpense.Splits.First().PersonId.Should().Be(8);
            capturedSharedExpense.Splits.First().Amount.Should().Be(10000m);

            capturedMovement.Should().NotBeNull();
            capturedMovement!.TransactionId.Should().Be(50);
            capturedMovement.SharedExpenseId.Should().Be(70);
            capturedMovement.Shares.Should().HaveCount(2);
            capturedMovement.Shares.First(s => s.PersonId == 8).SharedExpenseSplitId.Should().Be(1);

            result.TotalAmount.Should().Be(30000m);
        }

        [Fact]
        public async Task CreateMovementAsync_PaidByUserWithCard_ComputesInstallmentSplitAmount()
        {
            var sharedEvent = BuildEvent(Juan);
            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var transactionClass = new TransactionClass { Id = 5, UserId = UserId, Description = "Comida", IncExp = "E" };
            var card = new Card { Id = 3, UserId = UserId };

            var dto = new SharedEventMovementAddDTO
            {
                Date = new DateTime(2026, 5, 25),
                Description = "Compra grande",
                TransactionClassId = 5,
                AssetId = 1,
                TotalAmount = 1200m,
                PayerPersonId = null,
                Shares = new List<SharedEventMovementShareInputDTO>
                {
                    new() { PersonId = null, Amount = 900m },
                    new() { PersonId = 8, Amount = 300m }
                },
                Payment = new SharedEventMovementPaymentInputDTO
                {
                    CardId = 3,
                    Installments = 6,
                    FirstInstallment = new DateTime(2026, 6, 1)
                }
            };

            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);
            _assetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
            _assetUserRepoMock.Setup(r => r.GetUserAssetAsync(UserId, 1)).ReturnsAsync(new Asset_User());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(transactionClass);
            _cardRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(card);
            _cardTransactionServiceMock.Setup(s => s.AddCardTransactionAsync(UserId, It.IsAny<CardTransactionAddDTO>()))
                .ReturnsAsync(60);

            SharedExpense? capturedSharedExpense = null;
            _sharedExpenseRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<SharedExpense>()))
                .Callback<SharedExpense>(se => capturedSharedExpense = se)
                .ReturnsAsync((SharedExpense se) =>
                {
                    se.Id = 71;
                    var i = 1;
                    foreach (var split in se.Splits) split.Id = i++;
                    return se;
                });

            SharedEventMovement? capturedMovement = null;
            _sharedEventMovementRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<SharedEventMovement>()))
                .Callback<SharedEventMovement>(m => capturedMovement = m)
                .ReturnsAsync((SharedEventMovement m) => { m.Id = 901; return m; });
            _sharedEventMovementRepoMock.Setup(r => r.GetDetailByIdAsync(901))
                .ReturnsAsync(() => capturedMovement);

            await _sut.CreateMovementAsync(UserId, EventId, dto);

            capturedMovement.Should().NotBeNull();
            capturedMovement!.CardTransactionId.Should().Be(60);
            capturedSharedExpense!.Splits.First().InstallmentSplitAmount.Should().Be(50m); // 300 / 6
            _cardTransactionServiceMock.Verify(s => s.AddCardTransactionAsync(UserId, It.Is<CardTransactionAddDTO>(
                d => d.Installments == 6 && d.TotalAmount == 1200m)), Times.Once);
        }

        [Fact]
        public async Task CreateMovementAsync_PaidByThirdParty_CreatesNoDerivedRecords()
        {
            var sharedEvent = BuildEvent(Juan);
            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var transactionClass = new TransactionClass { Id = 5, UserId = UserId, Description = "Combustible", IncExp = "E" };

            var dto = new SharedEventMovementAddDTO
            {
                Date = new DateTime(2026, 5, 25),
                Description = "Nafta",
                TransactionClassId = 5,
                AssetId = 1,
                TotalAmount = 15000m,
                PayerPersonId = 8,
                Shares = new List<SharedEventMovementShareInputDTO>
                {
                    new() { PersonId = null, Amount = 5000m },
                    new() { PersonId = 8, Amount = 10000m }
                },
                Payment = null
            };

            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);
            _assetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
            _assetUserRepoMock.Setup(r => r.GetUserAssetAsync(UserId, 1)).ReturnsAsync(new Asset_User());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(transactionClass);

            SharedEventMovement? capturedMovement = null;
            _sharedEventMovementRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<SharedEventMovement>()))
                .Callback<SharedEventMovement>(m => capturedMovement = m)
                .ReturnsAsync((SharedEventMovement m) => { m.Id = 902; return m; });
            _sharedEventMovementRepoMock.Setup(r => r.GetDetailByIdAsync(902))
                .ReturnsAsync(() => capturedMovement);

            await _sut.CreateMovementAsync(UserId, EventId, dto);

            capturedMovement.Should().NotBeNull();
            capturedMovement!.TransactionId.Should().BeNull();
            capturedMovement.CardTransactionId.Should().BeNull();
            capturedMovement.SharedExpenseId.Should().BeNull();
            capturedMovement.Shares.First(s => s.PersonId == null).AmountSettled.Should().Be(0);

            _transactionRepoMock.Verify(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()), Times.Never);
            _cardTransactionServiceMock.Verify(s => s.AddCardTransactionAsync(It.IsAny<int>(), It.IsAny<CardTransactionAddDTO>()), Times.Never);
            _sharedExpenseRepoMock.Verify(r => r.AddAsyncReturnObject(It.IsAny<SharedExpense>()), Times.Never);
        }

        [Fact]
        public async Task CreateMovementAsync_SharesSumMismatch_ThrowsBusinessRuleException()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var dto = new SharedEventMovementAddDTO
            {
                Date = DateTime.Today,
                Description = "Test",
                TransactionClassId = 5,
                AssetId = 1,
                TotalAmount = 1000m,
                PayerPersonId = null,
                Shares = new List<SharedEventMovementShareInputDTO> { new() { PersonId = null, Amount = 500m } },
                Payment = new SharedEventMovementPaymentInputDTO { AccountId = 2 }
            };

            await FluentActions.Invoking(() => _sut.CreateMovementAsync(UserId, EventId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateMovementAsync_PayerNotParticipant_ThrowsBusinessRuleException()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var dto = new SharedEventMovementAddDTO
            {
                Date = DateTime.Today,
                Description = "Test",
                TransactionClassId = 5,
                AssetId = 1,
                TotalAmount = 1000m,
                PayerPersonId = 999, // no es participante
                Shares = new List<SharedEventMovementShareInputDTO> { new() { PersonId = null, Amount = 1000m } }
            };

            await FluentActions.Invoking(() => _sut.CreateMovementAsync(UserId, EventId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        // ── Balances (D1) ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ComputesBalances_ThatSumToZero()
        {
            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var transactionClass = new TransactionClass { Id = 5, UserId = UserId, Description = "Comida", IncExp = "E" };

            var movement1 = new SharedEventMovement
            {
                Id = 1,
                AssetId = 1,
                Asset = asset,
                TransactionClassId = 5,
                TransactionClass = transactionClass,
                TotalAmount = 30000m,
                PayerPersonId = null,
                Shares = new List<SharedEventMovementShare>
                {
                    new() { PersonId = null, Amount = 10000m },
                    new() { PersonId = 8, Amount = 10000m, Person = Juan },
                    new() { PersonId = 9, Amount = 10000m, Person = Pedro }
                }
            };
            var movement2 = new SharedEventMovement
            {
                Id = 2,
                AssetId = 1,
                Asset = asset,
                TransactionClassId = 5,
                TransactionClass = transactionClass,
                TotalAmount = 15000m,
                PayerPersonId = 8,
                PayerPerson = Juan,
                Shares = new List<SharedEventMovementShare>
                {
                    new() { PersonId = null, Amount = 5000m },
                    new() { PersonId = 8, Amount = 10000m, Person = Juan }
                }
            };

            var sharedEvent = BuildEvent(Juan, Pedro);
            sharedEvent.Movements = new List<SharedEventMovement> { movement1, movement2 };

            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var result = await _sut.GetByIdAsync(UserId, EventId);

            result.Balances.Sum(b => b.NetBalance).Should().Be(0);
            result.Balances.First(b => b.PersonId == null).NetBalance.Should().Be(15000m);
            result.Balances.First(b => b.PersonId == 8).NetBalance.Should().Be(-5000m);
            result.Balances.First(b => b.PersonId == 9).NetBalance.Should().Be(-10000m);
        }

        // ── Resumen consolidado ───────────────────────────────────────────────

        [Fact]
        public async Task GetActiveSummaryAsync_ReturnsOnlyUsersOwnBalancePerOpenEvent()
        {
            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var transactionClass = new TransactionClass { Id = 5, UserId = UserId, Description = "Comida" };

            var movement = new SharedEventMovement
            {
                Id = 1, AssetId = 1, Asset = asset, TransactionClassId = 5, TransactionClass = transactionClass,
                TotalAmount = 15000m, PayerPersonId = null,
                Shares = new List<SharedEventMovementShare>
                {
                    new() { PersonId = null, Amount = 10000m },
                    new() { PersonId = 8, Amount = 5000m, Person = Juan }
                }
            };

            var sharedEvent = BuildEvent(Juan);
            sharedEvent.Movements = new List<SharedEventMovement> { movement };

            _sharedEventRepoMock.Setup(r => r.GetOpenEventsDetailAsync(UserId))
                .ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = (await _sut.GetActiveSummaryAsync(UserId)).ToList();

            result.Should().ContainSingle();
            result[0].EventId.Should().Be(EventId);
            result[0].Balances.Should().ContainSingle(b => b.AssetId == 1 && b.MyBalance == 5000m); // 15000 - 10000
        }

        [Fact]
        public async Task GetConsolidatedDebtsAsync_CombinesEventBalanceWithLooseV1Debt()
        {
            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var transactionClass = new TransactionClass { Id = 5, UserId = UserId, Description = "Comida" };

            // evento: Juan debe 5000 al usuario (no puso nada, consumió 5000)
            var movement = new SharedEventMovement
            {
                Id = 1, AssetId = 1, Asset = asset, TransactionClassId = 5, TransactionClass = transactionClass,
                TotalAmount = 5000m, PayerPersonId = null,
                Shares = new List<SharedEventMovementShare> { new() { PersonId = 8, Amount = 5000m, Person = Juan } }
            };
            var sharedEvent = BuildEvent(Juan);
            sharedEvent.Movements = new List<SharedEventMovement> { movement };

            _sharedEventRepoMock.Setup(r => r.GetOpenEventsDetailAsync(UserId))
                .ReturnsAsync(new List<SharedEvent> { sharedEvent });

            // suelto V1: Juan debe 2000 adicionales (gasto sin evento)
            var looseSplit = new SharedExpenseSplit
            {
                Id = 99, PersonId = 8, Person = Juan, Amount = 2000m, AmountReimbursed = 0,
                SharedExpense = new SharedExpense { Transaction = new Transaction { AssetId = 1 } }
            };
            _sharedExpenseRepoMock.Setup(r => r.GetPendingSplitsByUserIdAsync(UserId))
                .ReturnsAsync(new List<SharedExpenseSplit> { looseSplit });

            var result = (await _sut.GetConsolidatedDebtsAsync(UserId)).ToList();

            result.Should().ContainSingle();
            result[0].PersonId.Should().Be(8);
            result[0].AssetId.Should().Be(1);
            result[0].PendingInFavor.Should().Be(7000m); // 2000 suelto + 5000 del evento
            result[0].PendingAgainst.Should().Be(0m);
        }

        // ── Guardas de edición/borrado ────────────────────────────────────────

        [Fact]
        public async Task UpdateMovementAsync_WhenHasActivity_ThrowsBusinessRuleException()
        {
            var sharedEvent = BuildEvent(Juan);
            var movement = new SharedEventMovement { Id = 1, SharedEventId = EventId, Shares = new List<SharedEventMovementShare>() };

            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);
            _sharedEventMovementRepoMock.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(movement);
            _sharedEventMovementRepoMock.Setup(r => r.HasActivityAsync(1)).ReturnsAsync(true);

            var dto = new SharedEventMovementAddDTO
            {
                Date = DateTime.Today,
                Description = "Test",
                TransactionClassId = 5,
                AssetId = 1,
                TotalAmount = 1000m,
                Shares = new List<SharedEventMovementShareInputDTO> { new() { PersonId = null, Amount = 1000m } },
                Payment = new SharedEventMovementPaymentInputDTO { AccountId = 2 }
            };

            await FluentActions.Invoking(() => _sut.UpdateMovementAsync(UserId, EventId, 1, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task DeleteMovementAsync_WhenHasActivity_ThrowsBusinessRuleException()
        {
            var sharedEvent = BuildEvent(Juan);
            var movement = new SharedEventMovement { Id = 1, SharedEventId = EventId, Shares = new List<SharedEventMovementShare>() };

            _sharedEventRepoMock.Setup(r => r.GetByIdAsync(EventId)).ReturnsAsync(sharedEvent);
            _sharedEventMovementRepoMock.Setup(r => r.GetDetailByIdAsync(1)).ReturnsAsync(movement);
            _sharedEventMovementRepoMock.Setup(r => r.HasActivityAsync(1)).ReturnsAsync(true);

            await FluentActions.Invoking(() => _sut.DeleteMovementAsync(UserId, EventId, 1))
                .Should().ThrowAsync<BusinessRuleException>();
        }
    }
}
