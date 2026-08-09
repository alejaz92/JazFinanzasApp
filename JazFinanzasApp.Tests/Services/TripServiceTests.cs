using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Report;
using JazFinanzasApp.API.Business.DTO.Trip;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;
using System.Linq.Expressions;

namespace JazFinanzasApp.Tests.Services
{
    public class TripServiceTests
    {
        private readonly Mock<ITripRepository> _tripRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<ITripSuggestionDismissalRepository> _dismissalRepoMock;
        private readonly Mock<ISharedEventRepository> _sharedEventRepoMock;
        private readonly Mock<ISharedEventPaymentRepository> _sharedEventPaymentRepoMock;
        private readonly Mock<IReportService> _reportServiceMock;
        private readonly TripService _sut;

        private const int UserId = 1;

        public TripServiceTests()
        {
            _tripRepoMock = new Mock<ITripRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _dismissalRepoMock = new Mock<ITripSuggestionDismissalRepository>();
            _sharedEventRepoMock = new Mock<ISharedEventRepository>();

            // Defaults: sin movimientos ni descartes
            _transactionRepoMock.Setup(r => r.GetTransactionsByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(Enumerable.Empty<Transaction>());
            _cardTransactionRepoMock.Setup(r => r.GetCardTransactionsByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(Enumerable.Empty<CardTransaction>());
            _transactionRepoMock.Setup(r => r.GetTripSuggestibleTransactionsAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Enumerable.Empty<Transaction>());
            _cardTransactionRepoMock.Setup(r => r.GetTripSuggestibleCardTransactionsAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Enumerable.Empty<CardTransaction>());
            _dismissalRepoMock.Setup(r => r.GetByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(Enumerable.Empty<TripSuggestionDismissal>());
            _dismissalRepoMock.Setup(r => r.GetByTripAndMovementAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync((TripSuggestionDismissal?)null);
            _transactionRepoMock.Setup(r => r.SearchTripAssociableTransactionsAsync(UserId, It.IsAny<string?>()))
                .ReturnsAsync(Enumerable.Empty<Transaction>());
            _cardTransactionRepoMock.Setup(r => r.SearchTripAssociableCardTransactionsAsync(UserId, It.IsAny<string?>()))
                .ReturnsAsync(Enumerable.Empty<CardTransaction>());
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<SharedEvent>());

            _sharedEventPaymentRepoMock = new Mock<ISharedEventPaymentRepository>();
            _sharedEventPaymentRepoMock.Setup(r => r.GetSettlementAllocationsByTransactionIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new List<SharedEventPaymentAllocation>());

            _reportServiceMock = new Mock<IReportService>();
            _reportServiceMock.Setup(r => r.GetTripOwnAndGrossTotalsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new TripTotalsDTO());

            _sut = new TripService(
                _tripRepoMock.Object,
                _transactionRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _dismissalRepoMock.Object,
                _sharedEventRepoMock.Object,
                _sharedEventPaymentRepoMock.Object,
                _reportServiceMock.Object);
        }

        private static Trip BuildTrip(int id = 5, int userId = UserId) => new()
        {
            Id = id,
            Name = "Bariloche 2026",
            Type = "DOMESTIC",
            StartDate = DateTime.UtcNow.Date.AddDays(10),
            EndDate = DateTime.UtcNow.Date.AddDays(20),
            UserId = userId
        };

        private static Transaction BuildExpenseTransaction(int id = 10, int userId = UserId) => new()
        {
            Id = id,
            UserId = userId,
            MovementType = "E",
            Amount = -5000m,
            Date = DateTime.UtcNow.Date.AddDays(12),
            Detail = "Hotel",
            TransactionClassId = 3,
            TransactionClass = new TransactionClass { Id = 3, Description = "Hoteles", UserId = userId },
            Asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" }
        };

        private static CardTransaction BuildCardTransaction(int id = 20, int userId = UserId) => new()
        {
            Id = id,
            UserId = userId,
            TotalAmount = 120000m,
            Date = DateTime.UtcNow.Date.AddDays(11),
            Detail = "Vuelo",
            TransactionClassId = 4,
            TransactionClass = new TransactionClass { Id = 4, Description = "Vuelos", UserId = userId },
            Asset = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" }
        };

        private void SetupOwnedTrip(Trip trip)
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        }

        private void SetupNoDuplicates()
        {
            _tripRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
                .ReturnsAsync(Enumerable.Empty<Trip>());
        }

        private static TripAssociationsDTO SingleMovement(string type, int id) => new()
        {
            Movements = new List<TripMovementRefDTO> { new() { Type = type, Id = id } }
        };

        // ── GetAllForUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetAllForUserAsync_ReturnsMappedTrips()
        {
            var trips = new List<Trip> { BuildTrip(1), BuildTrip(2) };
            _tripRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(trips);

            var result = (await _sut.GetAllForUserAsync(UserId)).ToList();

            result.Should().HaveCount(2);
            result[0].Name.Should().Be("Bariloche 2026");
            result[0].Type.Should().Be("DOMESTIC");
        }

        // ── Estado derivado ───────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_TripInFuture_StatusIsPlanned()
        {
            var trip = BuildTrip();
            trip.StartDate = DateTime.UtcNow.Date.AddDays(5);
            trip.EndDate = DateTime.UtcNow.Date.AddDays(10);
            SetupOwnedTrip(trip);

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Status.Should().Be("PLANNED");
        }

        [Fact]
        public async Task GetByIdAsync_TripOngoing_StatusIsInProgress()
        {
            var trip = BuildTrip();
            trip.StartDate = DateTime.UtcNow.Date.AddDays(-2);
            trip.EndDate = DateTime.UtcNow.Date.AddDays(2);
            SetupOwnedTrip(trip);

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Status.Should().Be("IN_PROGRESS");
        }

        [Fact]
        public async Task GetByIdAsync_TripEndsToday_StatusIsInProgress()
        {
            var trip = BuildTrip();
            trip.StartDate = DateTime.UtcNow.Date.AddDays(-5);
            trip.EndDate = DateTime.UtcNow.Date;
            SetupOwnedTrip(trip);

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Status.Should().Be("IN_PROGRESS");
        }

        [Fact]
        public async Task GetByIdAsync_TripInPast_StatusIsFinished()
        {
            var trip = BuildTrip();
            trip.StartDate = DateTime.UtcNow.Date.AddDays(-10);
            trip.EndDate = DateTime.UtcNow.Date.AddDays(-5);
            SetupOwnedTrip(trip);

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Status.Should().Be("FINISHED");
        }

        // ── GetByIdAsync (validaciones y movimientos) ─────────────────────────

        [Fact]
        public async Task GetByIdAsync_TripNotFound_ThrowsNotFoundException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Trip?)null);

            await FluentActions.Invoking(() => _sut.GetByIdAsync(UserId, 99))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetByIdAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            SetupOwnedTrip(BuildTrip(5, userId: 2));

            await FluentActions.Invoking(() => _sut.GetByIdAsync(UserId, 5))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task GetByIdAsync_MergesAccountAndCardMovementsOrderedByDate()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction(); // día +12
            var cardTransaction = BuildCardTransaction(); // día +11
            _transactionRepoMock.Setup(r => r.GetTransactionsByTripIdAsync(5))
                .ReturnsAsync(new List<Transaction> { transaction });
            _cardTransactionRepoMock.Setup(r => r.GetCardTransactionsByTripIdAsync(5))
                .ReturnsAsync(new List<CardTransaction> { cardTransaction });

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.Movements.Should().HaveCount(2);
            result.Movements[0].Origin.Should().Be("CARD");
            result.Movements[0].Amount.Should().Be(120000m); // TotalAmount devengado
            result.Movements[1].Origin.Should().Be("ACCOUNT");
            result.Movements[1].Amount.Should().Be(5000m); // egreso en positivo
        }

        // ── GetByIdAsync (los dos totales) ──────────────────────────────────────
        // docs/plans/activos/plan-detalle-viaje-montos-propios.md, Fase 2

        [Fact]
        public async Task GetByIdAsync_ReturnsOwnAndGrossTotalsFromReportService()
        {
            SetupOwnedTrip(BuildTrip());
            _reportServiceMock.Setup(r => r.GetTripOwnAndGrossTotalsAsync(UserId, 5))
                .ReturnsAsync(new TripTotalsDTO { OwnTotal = 1507.18m, GrossTotal = 4639.74m });

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.OwnTotal.Should().Be(1507.18m);
            result.GrossTotal.Should().Be(4639.74m);
        }

        // ── GetByIdAsync (parte propia del Evento Compartido) ──────────────────
        // docs/plans/activos/plan-detalle-viaje-montos-propios.md, Fase 1

        [Fact]
        public async Task GetByIdAsync_MovementWithoutLinkedEvent_OwnAmountFieldsAreNull()
        {
            SetupOwnedTrip(BuildTrip());
            _transactionRepoMock.Setup(r => r.GetTransactionsByTripIdAsync(5))
                .ReturnsAsync(new List<Transaction> { BuildExpenseTransaction() });

            var result = await _sut.GetByIdAsync(UserId, 5);

            var movement = result.Movements.Single();
            movement.IsShared.Should().BeFalse();
            movement.OwnAmount.Should().BeNull();
            movement.SharedEventId.Should().BeNull();
            movement.SharedWith.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_CardMovementDirectlyLinkedToEvent_ReturnsOwnAmountAndSharedWith()
        {
            SetupOwnedTrip(BuildTrip());
            var cardTransaction = BuildCardTransaction(id: 20);
            _cardTransactionRepoMock.Setup(r => r.GetCardTransactionsByTripIdAsync(5))
                .ReturnsAsync(new List<CardTransaction> { cardTransaction });

            var redo = new Person { Id = 7, Name = "Redo" };
            var eventMovement = new SharedEventMovement
            {
                SharedEventId = 15,
                CardTransactionId = 20,
                TotalAmount = 80000m,
                Shares = new List<SharedEventMovementShare>
                {
                    new() { PersonId = null, Amount = 40000m },
                    new() { PersonId = 7, Person = redo, Amount = 40000m }
                }
            };
            var sharedEvent = new SharedEvent { Id = 15, Movements = new List<SharedEventMovement> { eventMovement } };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetByIdAsync(UserId, 5);

            var movement = result.Movements.Single();
            movement.IsShared.Should().BeTrue();
            movement.SharedEventId.Should().Be(15);
            movement.OwnAmount.Should().Be(40000m);
            movement.GrossAmount.Should().Be(80000m);
            movement.PaidByName.Should().BeNull(); // pagó el usuario (PayerPersonId null)
            movement.SharedWith.Should().BeEquivalentTo(new[] { "Redo" });
        }

        [Fact]
        public async Task GetByIdAsync_AccountMovementDirectlyLinkedToEvent_ReturnsOwnAmount()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction(id: 10);
            _transactionRepoMock.Setup(r => r.GetTransactionsByTripIdAsync(5))
                .ReturnsAsync(new List<Transaction> { transaction });

            var eventMovement = new SharedEventMovement
            {
                SharedEventId = 15,
                TransactionId = 10,
                Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 5000m } }
            };
            var sharedEvent = new SharedEvent { Id = 15, Movements = new List<SharedEventMovement> { eventMovement } };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            // La transacción del propio movimiento (sin CardTransactionId): resuelve el vínculo directo.
            _transactionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync(new List<Transaction> { transaction });

            var result = await _sut.GetByIdAsync(UserId, 5);

            var movement = result.Movements.Single();
            movement.IsShared.Should().BeTrue();
            movement.OwnAmount.Should().Be(5000m);
        }

        [Fact]
        public async Task GetByIdAsync_CardMovementSplitAcrossInstallmentTransactions_AggregatesOwnAmount()
        {
            // Caso real de Bariloche 2026: el consumo de tarjeta se repartió en un SharedEventMovement por
            // cuota, cada uno con TransactionId apuntando al pago de esa cuota (CardTransactionId null en el
            // movimiento), y la cuota a su vez apunta al CardTransaction real vía Transaction.CardTransactionId.
            SetupOwnedTrip(BuildTrip());
            var cardTransaction = BuildCardTransaction(id: 20);
            _cardTransactionRepoMock.Setup(r => r.GetCardTransactionsByTripIdAsync(5))
                .ReturnsAsync(new List<CardTransaction> { cardTransaction });

            var viole = new Person { Id = 8, Name = "Viole" };
            var installment1 = new SharedEventMovement
            {
                SharedEventId = 15,
                TransactionId = 101,
                Shares = new List<SharedEventMovementShare>
                {
                    new() { PersonId = null, Amount = 10000m },
                    new() { PersonId = 8, Person = viole, Amount = 10000m }
                }
            };
            var installment2 = new SharedEventMovement
            {
                SharedEventId = 15,
                TransactionId = 102,
                Shares = new List<SharedEventMovementShare>
                {
                    new() { PersonId = null, Amount = 15000m },
                    new() { PersonId = 8, Person = viole, Amount = 15000m }
                }
            };
            var sharedEvent = new SharedEvent { Id = 15, Movements = new List<SharedEventMovement> { installment1, installment2 } };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var cuota1 = new Transaction { Id = 101, CardTransactionId = 20 };
            var cuota2 = new Transaction { Id = 102, CardTransactionId = 20 };
            _transactionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync(new List<Transaction> { cuota1, cuota2 });

            var result = await _sut.GetByIdAsync(UserId, 5);

            var movement = result.Movements.Single();
            movement.IsShared.Should().BeTrue();
            movement.SharedEventId.Should().Be(15);
            movement.OwnAmount.Should().Be(25000m);
            movement.SharedWith.Should().BeEquivalentTo(new[] { "Viole" });
        }

        [Fact]
        public async Task GetByIdAsync_MovementPaidByOtherAndSettledLater_ResolvesViaPaymentAllocation()
        {
            // Caso real de Buenos Aires 2024: "Big Pons" lo pagó Renzo, así que el movimiento de Evento nunca
            // tuvo TransactionId/CardTransactionId propio. El único rastro en la cuenta del usuario es la
            // transacción de saldo de deuda ("(Evento: Buenos Aires 2024) Big Pons", ya neta de su parte), que
            // se ata al movimiento solo vía SharedEventPaymentAllocations.SharedEventMovementShareId.
            SetupOwnedTrip(BuildTrip());
            var settlementTransaction = BuildExpenseTransaction(id: 5886);
            settlementTransaction.Amount = -19483.33m;
            settlementTransaction.Detail = "(Evento: Buenos Aires 2024) Big Pons";
            _transactionRepoMock.Setup(r => r.GetTransactionsByTripIdAsync(5))
                .ReturnsAsync(new List<Transaction> { settlementTransaction });

            var renzo = new Person { Id = 3, Name = "Renzo" };
            var ownShare = new SharedEventMovementShare { Id = 1168, SharedEventMovementId = 341, PersonId = null, Amount = 19483.33m };
            var eventMovement = new SharedEventMovement
            {
                Id = 341,
                SharedEventId = 22,
                PayerPersonId = 3,
                PayerPerson = renzo,
                TotalAmount = 116900m,
                Shares = new List<SharedEventMovementShare>
                {
                    ownShare,
                    new() { PersonId = 3, Person = renzo, Amount = 19483.33m }
                }
            };
            var sharedEvent = new SharedEvent { Id = 22, Movements = new List<SharedEventMovement> { eventMovement } };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var allocation = new SharedEventPaymentAllocation
            {
                SharedEventMovementShareId = 1168,
                SharedEventMovementShare = ownShare,
                CreatedExpenseTransactionId = 5886
            };
            _sharedEventPaymentRepoMock.Setup(r => r.GetSettlementAllocationsByTransactionIdsAsync(
                    It.Is<IEnumerable<int>>(ids => ids.Contains(5886))))
                .ReturnsAsync(new List<SharedEventPaymentAllocation> { allocation });

            var result = await _sut.GetByIdAsync(UserId, 5);

            var movement = result.Movements.Single();
            movement.IsShared.Should().BeTrue();
            movement.SharedEventId.Should().Be(22);
            movement.OwnAmount.Should().Be(19483.33m);
            movement.GrossAmount.Should().Be(116900m);
            movement.PaidByName.Should().Be("Renzo");
            movement.SharedWith.Should().Contain("Renzo");
        }

        // ── GetByIdAsync (eventos vinculados) ──────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_WithNoLinkedEvents_LinkedEventsIsEmpty()
        {
            SetupOwnedTrip(BuildTrip());

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.LinkedEvents.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_WithOneLinkedEvent_ReturnsNameStateAndTotalsByAsset()
        {
            SetupOwnedTrip(BuildTrip());

            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var sharedEvent = new SharedEvent
            {
                Id = 15,
                Name = "Prueba wizard",
                IsClosed = false,
                Participants = new List<SharedEventParticipant> { new(), new() },
                Movements = new List<SharedEventMovement>
                {
                    new() { AssetId = 1, Asset = asset, TotalAmount = 15000m },
                    new() { AssetId = 1, Asset = asset, TotalAmount = 6000m }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.LinkedEvents.Should().ContainSingle();
            var linked = result.LinkedEvents[0];
            linked.Id.Should().Be(15);
            linked.Name.Should().Be("Prueba wizard");
            linked.IsClosed.Should().BeFalse();
            linked.ParticipantCount.Should().Be(2);
            linked.MovementCount.Should().Be(2);
            linked.Totals.Should().ContainSingle(t => t.AssetId == 1 && t.AssetSymbol == "ARS" && t.Amount == 21000m);
        }

        [Fact]
        public async Task GetByIdAsync_WithTwoLinkedEvents_ReturnsBothWithTheirOwnState()
        {
            SetupOwnedTrip(BuildTrip());

            var asset = new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
            var openEvent = new SharedEvent { Id = 15, Name = "Vuelos", IsClosed = false, Participants = new List<SharedEventParticipant>(), Movements = new List<SharedEventMovement> { new() { AssetId = 1, Asset = asset, TotalAmount = 1000m } } };
            var closedEvent = new SharedEvent { Id = 16, Name = "Hospedaje", IsClosed = true, Participants = new List<SharedEventParticipant>(), Movements = new List<SharedEventMovement>() };
            _sharedEventRepoMock.Setup(r => r.GetDetailByTripIdAsync(5)).ReturnsAsync(new List<SharedEvent> { openEvent, closedEvent });

            var result = await _sut.GetByIdAsync(UserId, 5);

            result.LinkedEvents.Should().HaveCount(2);
            result.LinkedEvents.Should().ContainSingle(e => e.Id == 15 && !e.IsClosed && e.Totals.Single().Amount == 1000m);
            result.LinkedEvents.Should().ContainSingle(e => e.Id == 16 && e.IsClosed && e.MovementCount == 0 && !e.Totals.Any());
        }

        // ── CreateTripAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateTripAsync_ValidTrip_CreatesAndReturnsDTO()
        {
            SetupNoDuplicates();
            var dto = new TripAddDTO
            {
                Name = "Japon 2027",
                Type = "INTERNATIONAL",
                StartDate = new DateTime(2027, 3, 1),
                EndDate = new DateTime(2027, 3, 20)
            };

            Trip? captured = null;
            _tripRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Trip>()))
                .Callback<Trip>(t => captured = t)
                .ReturnsAsync((Trip t) => t);

            var result = await _sut.CreateTripAsync(UserId, dto);

            captured.Should().NotBeNull();
            captured!.UserId.Should().Be(UserId);
            captured.Name.Should().Be("Japon 2027");
            result.Type.Should().Be("INTERNATIONAL");
            result.Status.Should().Be("PLANNED");
        }

        [Fact]
        public async Task CreateTripAsync_EndDateBeforeStartDate_ThrowsBusinessRuleException()
        {
            var dto = new TripAddDTO
            {
                Name = "Viaje",
                Type = "DOMESTIC",
                StartDate = new DateTime(2027, 3, 20),
                EndDate = new DateTime(2027, 3, 1)
            };

            await FluentActions.Invoking(() => _sut.CreateTripAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateTripAsync_InvalidType_ThrowsBusinessRuleException()
        {
            var dto = new TripAddDTO
            {
                Name = "Viaje",
                Type = "OTRO",
                StartDate = new DateTime(2027, 3, 1),
                EndDate = new DateTime(2027, 3, 20)
            };

            await FluentActions.Invoking(() => _sut.CreateTripAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateTripAsync_DuplicateName_ThrowsBusinessRuleException()
        {
            _tripRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
                .ReturnsAsync(new List<Trip> { BuildTrip() });
            var dto = new TripAddDTO
            {
                Name = "Bariloche 2026",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 9, 1),
                EndDate = new DateTime(2026, 9, 10)
            };

            await FluentActions.Invoking(() => _sut.CreateTripAsync(UserId, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CreateTripAsync_SingleDayTrip_IsValid()
        {
            SetupNoDuplicates();
            var date = new DateTime(2027, 3, 1);
            var dto = new TripAddDTO { Name = "Escapada", Type = "DOMESTIC", StartDate = date, EndDate = date };

            _tripRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Trip>()))
                .ReturnsAsync((Trip t) => t);

            var result = await _sut.CreateTripAsync(UserId, dto);

            result.Name.Should().Be("Escapada");
        }

        // ── UpdateTripAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateTripAsync_ValidChanges_UpdatesTrip()
        {
            SetupOwnedTrip(BuildTrip());
            SetupNoDuplicates();
            var dto = new TripEditDTO
            {
                Name = "Bariloche invierno 2026",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 15)
            };

            await _sut.UpdateTripAsync(UserId, 5, dto);

            _tripRepoMock.Verify(r => r.UpdateAsync(It.Is<Trip>(t =>
                t.Id == 5 &&
                t.Name == "Bariloche invierno 2026" &&
                t.StartDate == new DateTime(2026, 8, 1) &&
                t.EndDate == new DateTime(2026, 8, 15))), Times.Once);
        }

        [Fact]
        public async Task UpdateTripAsync_TripNotFound_ThrowsNotFoundException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Trip?)null);
            var dto = new TripEditDTO
            {
                Name = "Viaje",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 15)
            };

            await FluentActions.Invoking(() => _sut.UpdateTripAsync(UserId, 99, dto))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateTripAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            SetupOwnedTrip(BuildTrip(5, userId: 2));
            var dto = new TripEditDTO
            {
                Name = "Viaje",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 15)
            };

            await FluentActions.Invoking(() => _sut.UpdateTripAsync(UserId, 5, dto))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task UpdateTripAsync_EndDateBeforeStartDate_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            var dto = new TripEditDTO
            {
                Name = "Viaje",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 15),
                EndDate = new DateTime(2026, 8, 1)
            };

            await FluentActions.Invoking(() => _sut.UpdateTripAsync(UserId, 5, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task UpdateTripAsync_DuplicateName_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            _tripRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
                .ReturnsAsync(new List<Trip> { BuildTrip(6) });
            var dto = new TripEditDTO
            {
                Name = "Bariloche 2026",
                Type = "DOMESTIC",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 15)
            };

            await FluentActions.Invoking(() => _sut.UpdateTripAsync(UserId, 5, dto))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        // ── DeleteTripAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteTripAsync_ExistingTrip_DeletesIt()
        {
            SetupOwnedTrip(BuildTrip());

            await _sut.DeleteTripAsync(UserId, 5);

            _tripRepoMock.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task DeleteTripAsync_TripNotFound_ThrowsNotFoundException()
        {
            _tripRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Trip?)null);

            await FluentActions.Invoking(() => _sut.DeleteTripAsync(UserId, 99))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteTripAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            SetupOwnedTrip(BuildTrip(5, userId: 2));

            await FluentActions.Invoking(() => _sut.DeleteTripAsync(UserId, 5))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── AssociateMovementsAsync ───────────────────────────────────────────

        [Fact]
        public async Task AssociateMovementsAsync_AccountExpense_SetsTripId()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10));

            _transactionRepoMock.Verify(r => r.UpdateAsync(It.Is<Transaction>(t => t.TripId == 5)), Times.Once);
        }

        [Fact]
        public async Task AssociateMovementsAsync_CardTransaction_SetsTripId()
        {
            SetupOwnedTrip(BuildTrip());
            var cardTransaction = BuildCardTransaction();
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);

            await _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("CARD", 20));

            _cardTransactionRepoMock.Verify(r => r.UpdateAsync(It.Is<CardTransaction>(ct => ct.TripId == 5)), Times.Once);
        }

        [Fact]
        public async Task AssociateMovementsAsync_RemovesExistingDismissal()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);
            _dismissalRepoMock.Setup(r => r.GetByTripAndMovementAsync(5, 10, null))
                .ReturnsAsync(new TripSuggestionDismissal { Id = 77, TripId = 5, TransactionId = 10, UserId = UserId });

            await _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10));

            _dismissalRepoMock.Verify(r => r.DeleteAsync(77), Times.Once);
        }

        [Fact]
        public async Task AssociateMovementsAsync_TransactionOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            SetupOwnedTrip(BuildTrip());
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10))
                .ReturnsAsync(BuildExpenseTransaction(10, userId: 2));

            await FluentActions.Invoking(() => _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10)))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task AssociateMovementsAsync_AlreadyInAnotherTrip_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            transaction.TripId = 8;
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await FluentActions.Invoking(() => _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10)))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task AssociateMovementsAsync_AlreadyInSameTrip_IsNoOp()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            transaction.TripId = 5;
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10));

            _transactionRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task AssociateMovementsAsync_CardPaymentTransaction_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            transaction.CardTransactionId = 33; // pago de cuota: excluido
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await FluentActions.Invoking(() => _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10)))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task AssociateMovementsAsync_LegacyCardPaymentDetail_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            transaction.Detail = "(Tarjeta | 2/12) Vuelo a Bariloche"; // pago legacy sin FK: excluido
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await FluentActions.Invoking(() => _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10)))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task AssociateMovementsAsync_ExcludedTransactionClass_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            transaction.TransactionClass = new TransactionClass { Id = 9, Description = "Gastos Tarjeta", UserId = UserId };
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await FluentActions.Invoking(() => _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10)))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task AssociateMovementsAsync_NonExpenseMovement_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            transaction.MovementType = "I";
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await FluentActions.Invoking(() => _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10)))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task AssociateMovementsAsync_InvalidMovementType_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());

            await FluentActions.Invoking(() => _sut.AssociateMovementsAsync(UserId, 5, SingleMovement("OTRO", 10)))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        // ── DisassociateMovementsAsync ────────────────────────────────────────

        [Fact]
        public async Task DisassociateMovementsAsync_MovementInTrip_ClearsTripId()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            transaction.TripId = 5;
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await _sut.DisassociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10));

            _transactionRepoMock.Verify(r => r.UpdateAsync(It.Is<Transaction>(t => t.TripId == null)), Times.Once);
        }

        [Fact]
        public async Task DisassociateMovementsAsync_MovementNotInTrip_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction(); // TripId null
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await FluentActions.Invoking(() => _sut.DisassociateMovementsAsync(UserId, 5, SingleMovement("ACCOUNT", 10)))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task DisassociateMovementsAsync_CardInTrip_ClearsTripId()
        {
            SetupOwnedTrip(BuildTrip());
            var cardTransaction = BuildCardTransaction();
            cardTransaction.TripId = 5;
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(cardTransaction);

            await _sut.DisassociateMovementsAsync(UserId, 5, SingleMovement("CARD", 20));

            _cardTransactionRepoMock.Verify(r => r.UpdateAsync(It.Is<CardTransaction>(ct => ct.TripId == null)), Times.Once);
        }

        // ── GetSuggestionsAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetSuggestionsAsync_MergesBothOriginsOrderedByDate()
        {
            SetupOwnedTrip(BuildTrip());
            _transactionRepoMock.Setup(r => r.GetTripSuggestibleTransactionsAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Transaction> { BuildExpenseTransaction() }); // día +12
            _cardTransactionRepoMock.Setup(r => r.GetTripSuggestibleCardTransactionsAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CardTransaction> { BuildCardTransaction() }); // día +11

            var result = (await _sut.GetSuggestionsAsync(UserId, 5)).ToList();

            result.Should().HaveCount(2);
            result[0].Origin.Should().Be("CARD");
            result[1].Origin.Should().Be("ACCOUNT");
        }

        [Fact]
        public async Task GetSuggestionsAsync_ExcludesDismissedMovements()
        {
            SetupOwnedTrip(BuildTrip());
            _transactionRepoMock.Setup(r => r.GetTripSuggestibleTransactionsAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Transaction> { BuildExpenseTransaction(10), BuildExpenseTransaction(11) });
            _cardTransactionRepoMock.Setup(r => r.GetTripSuggestibleCardTransactionsAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CardTransaction> { BuildCardTransaction(20) });
            _dismissalRepoMock.Setup(r => r.GetByTripIdAsync(5)).ReturnsAsync(new List<TripSuggestionDismissal>
            {
                new() { TripId = 5, TransactionId = 10, UserId = UserId },
                new() { TripId = 5, CardTransactionId = 20, UserId = UserId }
            });

            var result = (await _sut.GetSuggestionsAsync(UserId, 5)).ToList();

            result.Should().ContainSingle();
            result[0].Id.Should().Be(11);
            result[0].Origin.Should().Be("ACCOUNT");
        }

        // ── SearchAssociableMovementsAsync ────────────────────────────────────

        [Fact]
        public async Task SearchAssociableMovementsAsync_MergesBothOriginsOrderedByDateDescending()
        {
            SetupOwnedTrip(BuildTrip());
            _transactionRepoMock.Setup(r => r.SearchTripAssociableTransactionsAsync(UserId, "hotel"))
                .ReturnsAsync(new List<Transaction> { BuildExpenseTransaction() }); // día +12
            _cardTransactionRepoMock.Setup(r => r.SearchTripAssociableCardTransactionsAsync(UserId, "hotel"))
                .ReturnsAsync(new List<CardTransaction> { BuildCardTransaction() }); // día +11

            var result = (await _sut.SearchAssociableMovementsAsync(UserId, 5, "hotel")).ToList();

            result.Should().HaveCount(2);
            result[0].Origin.Should().Be("ACCOUNT"); // más reciente primero
            result[1].Origin.Should().Be("CARD");
        }

        [Fact]
        public async Task SearchAssociableMovementsAsync_TripOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            SetupOwnedTrip(BuildTrip(5, userId: 2));

            await FluentActions.Invoking(() => _sut.SearchAssociableMovementsAsync(UserId, 5, null))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── DismissSuggestionAsync ────────────────────────────────────────────

        [Fact]
        public async Task DismissSuggestionAsync_AccountMovement_PersistsDismissal()
        {
            SetupOwnedTrip(BuildTrip());
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(BuildExpenseTransaction());

            await _sut.DismissSuggestionAsync(UserId, 5, new TripMovementRefDTO { Type = "ACCOUNT", Id = 10 });

            _dismissalRepoMock.Verify(r => r.AddAsync(It.Is<TripSuggestionDismissal>(d =>
                d.TripId == 5 && d.TransactionId == 10 && d.CardTransactionId == null && d.UserId == UserId)), Times.Once);
        }

        [Fact]
        public async Task DismissSuggestionAsync_CardMovement_PersistsDismissal()
        {
            SetupOwnedTrip(BuildTrip());
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(BuildCardTransaction());

            await _sut.DismissSuggestionAsync(UserId, 5, new TripMovementRefDTO { Type = "CARD", Id = 20 });

            _dismissalRepoMock.Verify(r => r.AddAsync(It.Is<TripSuggestionDismissal>(d =>
                d.TripId == 5 && d.CardTransactionId == 20 && d.TransactionId == null)), Times.Once);
        }

        [Fact]
        public async Task DismissSuggestionAsync_AlreadyDismissed_IsIdempotent()
        {
            SetupOwnedTrip(BuildTrip());
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(BuildExpenseTransaction());
            _dismissalRepoMock.Setup(r => r.GetByTripAndMovementAsync(5, 10, null))
                .ReturnsAsync(new TripSuggestionDismissal { Id = 77, TripId = 5, TransactionId = 10, UserId = UserId });

            await _sut.DismissSuggestionAsync(UserId, 5, new TripMovementRefDTO { Type = "ACCOUNT", Id = 10 });

            _dismissalRepoMock.Verify(r => r.AddAsync(It.IsAny<TripSuggestionDismissal>()), Times.Never);
        }

        [Fact]
        public async Task DismissSuggestionAsync_MovementAlreadyInTrip_ThrowsBusinessRuleException()
        {
            SetupOwnedTrip(BuildTrip());
            var transaction = BuildExpenseTransaction();
            transaction.TripId = 8;
            _transactionRepoMock.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(transaction);

            await FluentActions.Invoking(() => _sut.DismissSuggestionAsync(UserId, 5, new TripMovementRefDTO { Type = "ACCOUNT", Id = 10 }))
                .Should().ThrowAsync<BusinessRuleException>();
        }
    }
}
