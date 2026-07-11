using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class SharedEventPaymentServiceTests
    {
        private readonly Mock<ISharedEventRepository> _sharedEventRepoMock;
        private readonly Mock<ISharedEventMovementRepository> _sharedEventMovementRepoMock;
        private readonly Mock<ISharedEventPaymentRepository> _sharedEventPaymentRepoMock;
        private readonly Mock<ISharedExpenseRepository> _sharedExpenseRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly SharedEventPaymentService _sut;

        private const int UserId = 1;
        private const int EventId = 100;

        private static readonly Person Juan = new() { Id = 8, UserId = UserId, Name = "Juan" };
        private static readonly Person Pedro = new() { Id = 9, UserId = UserId, Name = "Pedro" };

        public SharedEventPaymentServiceTests()
        {
            _sharedEventRepoMock = new Mock<ISharedEventRepository>();
            _sharedEventMovementRepoMock = new Mock<ISharedEventMovementRepository>();
            _sharedEventPaymentRepoMock = new Mock<ISharedEventPaymentRepository>();
            _sharedExpenseRepoMock = new Mock<ISharedExpenseRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _sut = new SharedEventPaymentService(
                _sharedEventRepoMock.Object,
                _sharedEventMovementRepoMock.Object,
                _sharedEventPaymentRepoMock.Object,
                _sharedExpenseRepoMock.Object,
                _transactionRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _transactionClassRepoMock.Object,
                _assetRepoMock.Object,
                _accountRepoMock.Object,
                _portfolioRepoMock.Object,
                _unitOfWorkMock.Object);

            _assetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" });
            _accountRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Account { Id = 2, UserId = UserId });
            _portfolioRepoMock.Setup(r => r.GetDefaultPortfolio(UserId)).ReturnsAsync(new Portfolio { Id = 1, UserId = UserId });
        }

        private static SharedEvent BuildEvent(params Person[] people)
        {
            return new SharedEvent
            {
                Id = EventId,
                UserId = UserId,
                Name = "Asado",
                IsClosed = false,
                Participants = people.Select(p => new SharedEventParticipant { PersonId = p.Id, Person = p }).ToList()
            };
        }

        private void SetupCapturedTransactions(out List<Transaction> created)
        {
            var capturedList = new List<Transaction>();
            _transactionRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => capturedList.Add(t))
                .ReturnsAsync((Transaction t) => { t.Id = 1000 + capturedList.Count; return t; });
            created = capturedList;
        }

        private void SetupPaymentCapture(out Func<SharedEventPayment?> getCaptured)
        {
            SharedEventPayment? captured = null;
            _sharedEventPaymentRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<SharedEventPayment>()))
                .Callback<SharedEventPayment>(p => captured = p)
                .ReturnsAsync((SharedEventPayment p) => { p.Id = 900; return p; });
            _sharedEventPaymentRepoMock.Setup(r => r.GetDetailByIdAsync(900)).ReturnsAsync(() => captured);
            getCaptured = () => captured;
        }

        // ── Ejemplo multilateral de D2 ────────────────────────────────────────

        [Fact]
        public async Task CreatePaymentAsync_MultilateralExample_ReducesCarneAndCreatesCombustibleExpense()
        {
            var sharedEvent = BuildEvent(Juan, Pedro);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var carneTransaction = new Transaction { Id = 500, AccountId = 2, AssetId = 1, Amount = -30000m };
            var carneSharedExpense = new SharedExpense
            {
                Id = 50,
                TransactionId = 500,
                Splits = new List<SharedExpenseSplit>
                {
                    new() { Id = 10, PersonId = 8, Amount = 10000m, AmountReimbursed = 0, Status = SharedExpenseSplitStatus.Pending },
                    new() { Id = 11, PersonId = 9, Amount = 10000m, AmountReimbursed = 0, Status = SharedExpenseSplitStatus.Pending }
                }
            };
            var movementCarne = new SharedEventMovement
            {
                Id = 1, SharedEventId = EventId, Date = new DateTime(2026, 5, 25), Description = "Carne",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 30000m, PayerPersonId = null,
                TransactionId = 500, SharedExpenseId = 50, SharedExpense = carneSharedExpense
            };

            var movementCombustible = new SharedEventMovement
            {
                Id = 2, SharedEventId = EventId, Date = new DateTime(2026, 5, 25), Description = "Combustible",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 15000m, PayerPersonId = 8,
                Shares = new List<SharedEventMovementShare>
                {
                    new() { Id = 20, PersonId = null, Amount = 5000m, AmountSettled = 0 },
                    new() { Id = 21, PersonId = 8, Amount = 10000m, AmountSettled = 0 }
                }
            };

            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingCreditsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movementCarne });
            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingDebtsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movementCombustible });
            _transactionRepoMock.Setup(r => r.GetByIdAsync(500)).ReturnsAsync(carneTransaction);

            SetupCapturedTransactions(out var createdTransactions);
            SetupPaymentCapture(out var getCaptured);

            var dto = new SharedEventPaymentAddDTO
            {
                Date = new DateTime(2026, 6, 1),
                AssetId = 1,
                Amount = 15000m,
                FromPersonId = 9, // Pedro paga
                ToPersonId = null, // al usuario
                AccountId = 2
            };

            var result = await _sut.CreatePaymentAsync(UserId, EventId, dto);

            carneTransaction.Amount.Should().Be(-10000m); // -30000 + 10000(Juan) + 10000(Pedro)
            carneSharedExpense.Splits.First(s => s.PersonId == 8).AmountReimbursed.Should().Be(10000m);
            carneSharedExpense.Splits.First(s => s.PersonId == 9).AmountReimbursed.Should().Be(10000m);

            var debtExpense = createdTransactions.Should().ContainSingle(t => t.MovementType == "E" && t.Amount == -5000m).Subject;
            debtExpense.TransactionClassId.Should().Be(7);

            movementCombustible.Shares.First(s => s.PersonId == null).AmountSettled.Should().Be(5000m);

            result.Amount.Should().Be(15000m);
            getCaptured().Should().NotBeNull();
        }

        // ── Cobro parcial FIFO ────────────────────────────────────────────────

        [Fact]
        public async Task CreatePaymentAsync_PartialIncomingPayment_AppliesFifoToOldestCreditFirst()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var tx1 = new Transaction { Id = 501, AccountId = 2, AssetId = 1, Amount = -2000m };
            var tx2 = new Transaction { Id = 502, AccountId = 2, AssetId = 1, Amount = -2000m };

            var se1 = new SharedExpense { Id = 60, TransactionId = 501, Splits = new List<SharedExpenseSplit> { new() { Id = 30, PersonId = 8, Amount = 2000m, AmountReimbursed = 0 } } };
            var se2 = new SharedExpense { Id = 61, TransactionId = 502, Splits = new List<SharedExpenseSplit> { new() { Id = 31, PersonId = 8, Amount = 2000m, AmountReimbursed = 0 } } };

            var oldMovement = new SharedEventMovement { Id = 1, SharedEventId = EventId, Date = new DateTime(2026, 1, 1), Description = "Viejo", AssetId = 1, TransactionClassId = 7, TotalAmount = 2000m, SharedExpenseId = 60, SharedExpense = se1 };
            var newMovement = new SharedEventMovement { Id = 2, SharedEventId = EventId, Date = new DateTime(2026, 2, 1), Description = "Nuevo", AssetId = 1, TransactionClassId = 7, TotalAmount = 2000m, SharedExpenseId = 61, SharedExpense = se2 };

            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingCreditsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { oldMovement, newMovement });
            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingDebtsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement>());
            _transactionRepoMock.Setup(r => r.GetByIdAsync(501)).ReturnsAsync(tx1);
            _transactionRepoMock.Setup(r => r.GetByIdAsync(502)).ReturnsAsync(tx2);

            SetupCapturedTransactions(out _);
            SetupPaymentCapture(out _);

            var dto = new SharedEventPaymentAddDTO { Date = DateTime.Today, AssetId = 1, Amount = 2000m, FromPersonId = 8, ToPersonId = null, AccountId = 2 };

            await _sut.CreatePaymentAsync(UserId, EventId, dto);

            se1.Splits.First().AmountReimbursed.Should().Be(2000m); // el más viejo, saldado completo
            se2.Splits.First().AmountReimbursed.Should().Be(0m);     // el más nuevo, intacto
            tx1.Amount.Should().Be(0m);
            tx2.Amount.Should().Be(-2000m);
        }

        // ── Pago saliente ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreatePaymentAsync_OutgoingPayment_SettlesUserDebtWithNewExpense()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var movement = new SharedEventMovement
            {
                Id = 1, SharedEventId = EventId, Date = DateTime.Today, Description = "Nafta",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 15000m, PayerPersonId = 8,
                Shares = new List<SharedEventMovementShare> { new() { Id = 20, PersonId = null, Amount = 5000m, AmountSettled = 0 } }
            };

            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingCreditsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement>());
            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingDebtsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movement });

            SetupCapturedTransactions(out var createdTransactions);
            SetupPaymentCapture(out _);

            var dto = new SharedEventPaymentAddDTO { Date = DateTime.Today, AssetId = 1, Amount = 5000m, FromPersonId = null, ToPersonId = 8, AccountId = 2 };

            var result = await _sut.CreatePaymentAsync(UserId, EventId, dto);

            movement.Shares.First().AmountSettled.Should().Be(5000m);
            createdTransactions.Should().ContainSingle(t => t.Amount == -5000m && t.MovementType == "E");
            result.ToPersonId.Should().Be(8);
        }

        [Fact]
        public async Task CreatePaymentAsync_OutgoingPaymentExceedingDebt_ThrowsBusinessRuleException()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var movement = new SharedEventMovement
            {
                Id = 1, SharedEventId = EventId, Date = DateTime.Today, Description = "Nafta",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 5000m, PayerPersonId = 8,
                Shares = new List<SharedEventMovementShare> { new() { Id = 20, PersonId = null, Amount = 5000m, AmountSettled = 0 } }
            };

            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingCreditsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement>());
            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingDebtsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movement });

            var dto = new SharedEventPaymentAddDTO { Date = DateTime.Today, AssetId = 1, Amount = 10000m, FromPersonId = null, ToPersonId = 8, AccountId = 2 };

            await FluentActions.Invoking(() => _sut.CreatePaymentAsync(UserId, EventId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        // ── Compensación interna ──────────────────────────────────────────────

        [Fact]
        public async Task CreatePaymentAsync_InternalCompensation_SettlesCrossedItemsWithoutRealMoney()
        {
            var sharedEvent = BuildEvent(Juan, Pedro);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var carneTransaction = new Transaction { Id = 500, AccountId = 2, AssetId = 1, Amount = -5000m };
            var carneSharedExpense = new SharedExpense
            {
                Id = 50, TransactionId = 500,
                Splits = new List<SharedExpenseSplit> { new() { Id = 10, PersonId = 9, Amount = 5000m, AmountReimbursed = 0 } }
            };
            var movementCarne = new SharedEventMovement
            {
                Id = 1, SharedEventId = EventId, Date = DateTime.Today, Description = "Carne",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 5000m, SharedExpenseId = 50, SharedExpense = carneSharedExpense
            };

            var movementCombustible = new SharedEventMovement
            {
                Id = 2, SharedEventId = EventId, Date = DateTime.Today, Description = "Combustible",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 5000m, PayerPersonId = 8,
                Shares = new List<SharedEventMovementShare> { new() { Id = 20, PersonId = null, Amount = 5000m, AmountSettled = 0 } }
            };

            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingCreditsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movementCarne });
            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingDebtsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movementCombustible });
            _transactionRepoMock.Setup(r => r.GetByIdAsync(500)).ReturnsAsync(carneTransaction);

            SetupCapturedTransactions(out var createdTransactions);
            SetupPaymentCapture(out _);

            var dto = new SharedEventPaymentAddDTO
            {
                Date = DateTime.Today,
                AssetId = 1,
                Amount = 0,
                IsInternalCompensation = true,
                AccountId = 2
            };

            await _sut.CreatePaymentAsync(UserId, EventId, dto);

            carneTransaction.Amount.Should().Be(0m); // -5000 + 5000
            carneSharedExpense.Splits.First().AmountReimbursed.Should().Be(5000m);
            movementCombustible.Shares.First().AmountSettled.Should().Be(5000m);
            createdTransactions.Should().ContainSingle(t => t.Amount == -5000m); // el gasto de combustible categorizado
        }

        // ── Tarjeta: los 3 timings ────────────────────────────────────────────

        [Fact]
        public async Task CreatePaymentAsync_CardCredit_BeforeAnyInstallmentPaid_CreatesPlaceholderOnly()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var cardTransaction = new CardTransaction { Id = 70, AssetId = 1, Detail = "Compra" };
            var split = new SharedExpenseSplit { Id = 40, PersonId = 8, Amount = 600m, AmountReimbursed = 0, AmountApplied = 0, InstallmentSplitAmount = 100m };
            var sharedExpense = new SharedExpense { Id = 55, CardTransactionId = 70, Splits = new List<SharedExpenseSplit> { split } };
            var movement = new SharedEventMovement
            {
                Id = 1, SharedEventId = EventId, Date = DateTime.Today, Description = "Compra en cuotas",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 1200m, SharedExpenseId = 55, SharedExpense = sharedExpense
            };

            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingCreditsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movement });
            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingDebtsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement>());
            _transactionRepoMock.Setup(r => r.GetByCardTransactionIdAsync(70)).ReturnsAsync(new List<Transaction>()); // ninguna cuota pagada todavía
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(cardTransaction);
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Reintegro", UserId))
                .ReturnsAsync(new TransactionClass { Id = 74, UserId = UserId, Description = "Reintegro", IncExp = "I", IsSystem = true });

            SetupCapturedTransactions(out var createdTransactions);
            SetupPaymentCapture(out _);

            var dto = new SharedEventPaymentAddDTO { Date = DateTime.Today, AssetId = 1, Amount = 600m, FromPersonId = 8, ToPersonId = null, AccountId = 2 };

            await _sut.CreatePaymentAsync(UserId, EventId, dto);

            split.AmountApplied.Should().Be(0m); // nada aplicado todavía, todo a reserva
            split.AmountReimbursed.Should().Be(600m);
            createdTransactions.Should().ContainSingle(t => t.MovementType == "I" && t.Amount == 600m);
            _sharedExpenseRepoMock.Verify(r => r.AddReimbursementAsync(It.Is<SharedExpenseReimbursement>(rb => rb.Amount == 600m)), Times.Once);
        }

        [Fact]
        public async Task CreatePaymentAsync_CardCredit_AfterInstallmentsPaid_ReducesPaidInstallmentsDirectly()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var cardTransaction = new CardTransaction { Id = 70, AssetId = 1, Detail = "Compra" };
            var split = new SharedExpenseSplit { Id = 40, PersonId = 8, Amount = 600m, AmountReimbursed = 0, AmountApplied = 0, InstallmentSplitAmount = 100m };
            var sharedExpense = new SharedExpense { Id = 55, CardTransactionId = 70, Splits = new List<SharedExpenseSplit> { split } };
            var movement = new SharedEventMovement
            {
                Id = 1, SharedEventId = EventId, Date = DateTime.Today, Description = "Compra en cuotas",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 1200m, SharedExpenseId = 55, SharedExpense = sharedExpense
            };

            // ya se pagaron 2 cuotas (resumen ya corrido 2 veces), cada una en la misma cuenta 2
            var paidInstallment1 = new Transaction { Id = 601, AccountId = 2, AssetId = 1, Date = new DateTime(2026, 1, 1), Amount = -200m, CardTransactionId = 70 };
            var paidInstallment2 = new Transaction { Id = 602, AccountId = 2, AssetId = 1, Date = new DateTime(2026, 2, 1), Amount = -200m, CardTransactionId = 70 };

            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingCreditsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movement });
            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingDebtsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement>());
            _transactionRepoMock.Setup(r => r.GetByCardTransactionIdAsync(70))
                .ReturnsAsync(new List<Transaction> { paidInstallment1, paidInstallment2 });
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(cardTransaction);

            SetupCapturedTransactions(out var createdTransactions);
            SetupPaymentCapture(out _);

            // paga las 2 cuotas ya pagadas (100 cada una = 200), directCap = 2*100 - 0 = 200
            var dto = new SharedEventPaymentAddDTO { Date = DateTime.Today, AssetId = 1, Amount = 200m, FromPersonId = 8, ToPersonId = null, AccountId = 2 };

            await _sut.CreatePaymentAsync(UserId, EventId, dto);

            split.AmountApplied.Should().Be(200m);
            split.AmountReimbursed.Should().Be(200m);
            paidInstallment1.Amount.Should().Be(-100m); // -200 + 100
            paidInstallment2.Amount.Should().Be(-100m); // -200 + 100
            createdTransactions.Should().BeEmpty(); // nada a reserva, todo aplicado directo, sin cruce de cuenta
        }

        [Fact]
        public async Task CreatePaymentAsync_CardCredit_MixedTiming_AppliesDirectThenReserve()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var cardTransaction = new CardTransaction { Id = 70, AssetId = 1, Detail = "Compra" };
            var split = new SharedExpenseSplit { Id = 40, PersonId = 8, Amount = 600m, AmountReimbursed = 0, AmountApplied = 0, InstallmentSplitAmount = 100m };
            var sharedExpense = new SharedExpense { Id = 55, CardTransactionId = 70, Splits = new List<SharedExpenseSplit> { split } };
            var movement = new SharedEventMovement
            {
                Id = 1, SharedEventId = EventId, Date = DateTime.Today, Description = "Compra en cuotas",
                AssetId = 1, TransactionClassId = 7, TotalAmount = 1200m, SharedExpenseId = 55, SharedExpense = sharedExpense
            };

            var paidInstallment1 = new Transaction { Id = 601, AccountId = 2, AssetId = 1, Date = new DateTime(2026, 1, 1), Amount = -200m, CardTransactionId = 70 };

            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingCreditsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement> { movement });
            _sharedEventPaymentRepoMock.Setup(r => r.GetMovementsWithPendingDebtsAsync(EventId, 1))
                .ReturnsAsync(new List<SharedEventMovement>());
            _transactionRepoMock.Setup(r => r.GetByCardTransactionIdAsync(70))
                .ReturnsAsync(new List<Transaction> { paidInstallment1 }); // solo 1 cuota pagada
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(cardTransaction);
            _transactionClassRepoMock.Setup(r => r.GetTransactionClassByDescriptionAsync("Reintegro", UserId))
                .ReturnsAsync(new TransactionClass { Id = 74, UserId = UserId, Description = "Reintegro", IncExp = "I", IsSystem = true });

            SetupCapturedTransactions(out var createdTransactions);
            SetupPaymentCapture(out _);

            // paga 300: 100 va directo a la cuota ya pagada (directCap=100), el resto (200) a reserva
            var dto = new SharedEventPaymentAddDTO { Date = DateTime.Today, AssetId = 1, Amount = 300m, FromPersonId = 8, ToPersonId = null, AccountId = 2 };

            await _sut.CreatePaymentAsync(UserId, EventId, dto);

            split.AmountApplied.Should().Be(100m);
            split.AmountReimbursed.Should().Be(300m); // 100 directo + 200 a reserva
            paidInstallment1.Amount.Should().Be(-100m); // -200 + 100
            createdTransactions.Should().ContainSingle(t => t.MovementType == "I" && t.Amount == 200m);
        }

        // ── Reversa ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeletePaymentAsync_WhenNotLastPayment_ThrowsBusinessRuleException()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var payment = new SharedEventPayment { Id = 5, SharedEventId = EventId, Allocations = new List<SharedEventPaymentAllocation>() };
            _sharedEventPaymentRepoMock.Setup(r => r.GetDetailByIdAsync(5)).ReturnsAsync(payment);
            _sharedEventPaymentRepoMock.Setup(r => r.GetLastPaymentAsync(EventId)).ReturnsAsync(new SharedEventPayment { Id = 6 });

            await FluentActions.Invoking(() => _sut.DeletePaymentAsync(UserId, EventId, 5))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task DeletePaymentAsync_ReversesAccountCreditAllocation()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var split = new SharedExpenseSplit
            {
                Id = 10, PersonId = 8, Amount = 10000m, AmountReimbursed = 10000m,
                SharedExpense = new SharedExpense { Id = 50, TransactionId = 500 },
                Status = SharedExpenseSplitStatus.Paid
            };
            var touchedTransaction = new Transaction { Id = 500, AccountId = 2, AssetId = 1, Amount = -20000m }; // ya reducida por el pago

            var allocation = new SharedEventPaymentAllocation { Id = 1, SharedExpenseSplitId = 10, Amount = 10000m, TouchedTransactionId = 500 };
            var payment = new SharedEventPayment { Id = 5, SharedEventId = EventId, Allocations = new List<SharedEventPaymentAllocation> { allocation } };

            _sharedEventPaymentRepoMock.Setup(r => r.GetDetailByIdAsync(5)).ReturnsAsync(payment);
            _sharedEventPaymentRepoMock.Setup(r => r.GetLastPaymentAsync(EventId)).ReturnsAsync(payment);
            _sharedExpenseRepoMock.Setup(r => r.GetSplitByIdAsync(10)).ReturnsAsync(split);
            _transactionRepoMock.Setup(r => r.GetByIdAsync(500)).ReturnsAsync(touchedTransaction);

            await _sut.DeletePaymentAsync(UserId, EventId, 5);

            split.AmountReimbursed.Should().Be(0m);
            split.Status.Should().Be(SharedExpenseSplitStatus.Pending);
            touchedTransaction.Amount.Should().Be(-30000m); // -20000 - 10000, restaurado
            _sharedEventPaymentRepoMock.Verify(r => r.DeletePaymentWithAllocationsAsync(5), Times.Once);
        }

        [Fact]
        public async Task DeletePaymentAsync_ReversesDebtAllocation_DeletesCreatedExpense()
        {
            var sharedEvent = BuildEvent(Juan);
            _sharedEventRepoMock.Setup(r => r.GetDetailByIdAsync(EventId)).ReturnsAsync(sharedEvent);

            var share = new SharedEventMovementShare { Id = 20, PersonId = null, Amount = 5000m, AmountSettled = 5000m };
            var allocation = new SharedEventPaymentAllocation { Id = 2, SharedEventMovementShareId = 20, Amount = 5000m, CreatedExpenseTransactionId = 700 };
            var payment = new SharedEventPayment { Id = 6, SharedEventId = EventId, Allocations = new List<SharedEventPaymentAllocation> { allocation } };

            _sharedEventPaymentRepoMock.Setup(r => r.GetDetailByIdAsync(6)).ReturnsAsync(payment);
            _sharedEventPaymentRepoMock.Setup(r => r.GetLastPaymentAsync(EventId)).ReturnsAsync(payment);
            _sharedEventMovementRepoMock.Setup(r => r.GetShareByIdAsync(20)).ReturnsAsync(share);

            await _sut.DeletePaymentAsync(UserId, EventId, 6);

            share.AmountSettled.Should().Be(0m);
            _transactionRepoMock.Verify(r => r.DeleteAsync(700), Times.Once);
            _sharedEventPaymentRepoMock.Verify(r => r.DeletePaymentWithAllocationsAsync(6), Times.Once);
        }
    }
}
