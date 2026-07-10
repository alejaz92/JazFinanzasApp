using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Trip;
using JazFinanzasApp.API.Business.Exceptions;
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
        private readonly TripService _sut;

        private const int UserId = 1;

        public TripServiceTests()
        {
            _tripRepoMock = new Mock<ITripRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _dismissalRepoMock = new Mock<ITripSuggestionDismissalRepository>();

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

            _sut = new TripService(
                _tripRepoMock.Object,
                _transactionRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _dismissalRepoMock.Object);
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
