using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.DTO.SharedEvent.Import;
using JazFinanzasApp.API.Business.DTO.SharedExpense;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class SharedEventImportServiceTests
    {
        private readonly Mock<ISharedEventRepository> _sharedEventRepoMock;
        private readonly Mock<ISharedEventService> _sharedEventServiceMock;
        private readonly Mock<ISharedEventPaymentService> _sharedEventPaymentServiceMock;
        private readonly Mock<ISharedEventMovementRepository> _sharedEventMovementRepoMock;
        private readonly Mock<ISharedExpenseService> _sharedExpenseServiceMock;
        private readonly Mock<ISharedExpenseRepository> _sharedExpenseRepoMock;
        private readonly Mock<IPersonRepository> _personRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly Mock<IAssetRepository> _assetRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly SharedEventImportService _sut;

        private const int UserId = 1;
        private const int EventId = 100;
        private const int PepeId = 8;

        private static readonly Asset Ars = new() { Id = 1, Name = "Peso Argentino", Symbol = "ARS" };
        private static readonly TransactionClass Comida = new() { Id = 5, UserId = UserId, Description = "Comida", IncExp = "E" };

        private const string TwoRowCsv = """
            Fecha,Descripción,Categoría,Coste,Moneda,Yo,Pepe
            2026-01-10,Asado,Comida,10000.00,ARS,7000.00,-7000.00
            2026-01-11,Pepe me devolvió,Pago,7000.00,ARS,-7000.00,7000.00
            """;

        public SharedEventImportServiceTests()
        {
            _sharedEventRepoMock = new Mock<ISharedEventRepository>();
            _sharedEventServiceMock = new Mock<ISharedEventService>();
            _sharedEventPaymentServiceMock = new Mock<ISharedEventPaymentService>();
            _sharedEventMovementRepoMock = new Mock<ISharedEventMovementRepository>();
            _sharedExpenseServiceMock = new Mock<ISharedExpenseService>();
            _sharedExpenseRepoMock = new Mock<ISharedExpenseRepository>();
            _personRepoMock = new Mock<IPersonRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();
            _assetRepoMock = new Mock<IAssetRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _sut = new SharedEventImportService(
                _sharedEventRepoMock.Object,
                _sharedEventServiceMock.Object,
                _sharedEventPaymentServiceMock.Object,
                _sharedEventMovementRepoMock.Object,
                _sharedExpenseServiceMock.Object,
                _sharedExpenseRepoMock.Object,
                _personRepoMock.Object,
                _transactionClassRepoMock.Object,
                _assetRepoMock.Object,
                _transactionRepoMock.Object,
                _cardTransactionRepoMock.Object,
                _unitOfWorkMock.Object);

            _assetRepoMock.Setup(r => r.GetAssetsAsync()).ReturnsAsync(new List<Asset> { Ars });
        }

        private static SharedEvent BuildOpenEvent() => new() { Id = EventId, UserId = UserId, IsClosed = false, Participants = new List<SharedEventParticipant>() };

        private static SharedEventImportConfirmDTO BuildConfirmDto(
            List<SharedEventImportRowDecisionDTO>? rowDecisions = null,
            string csv = TwoRowCsv)
        {
            return new SharedEventImportConfirmDTO
            {
                CsvContent = csv,
                MemberMappings = new List<SharedEventImportMemberMappingDTO>
                {
                    new() { MemberName = "Yo", IsCurrentUser = true },
                    new() { MemberName = "Pepe", PersonId = PepeId }
                },
                CategoryMappings = new List<SharedEventImportCategoryMappingDTO>
                {
                    new() { CategoryName = "Comida", TransactionClassId = Comida.Id }
                },
                RowDecisions = rowDecisions ?? new List<SharedEventImportRowDecisionDTO>
                {
                    new() { RowIndex = 1, Action = SharedEventImportRowAction.CreateNew, AccountId = 2 },
                    new() { RowIndex = 2, Action = SharedEventImportRowAction.CreateNew, AccountId = 2 }
                }
            };
        }

        [Fact]
        public async Task ConfirmAsync_EventClosed_Throws()
        {
            var closedEvent = new SharedEvent { Id = EventId, UserId = UserId, IsClosed = true };
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(closedEvent);

            var act = () => _sut.ConfirmAsync(UserId, EventId, BuildConfirmDto());

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ConfirmAsync_EventOwnedByOtherUser_Throws()
        {
            var foreignEvent = new SharedEvent { Id = EventId, UserId = 999, IsClosed = false };
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(foreignEvent);

            var act = () => _sut.ConfirmAsync(UserId, EventId, BuildConfirmDto());

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task ConfirmAsync_NoCurrentUserMapping_Throws()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            var dto = BuildConfirmDto();
            dto.MemberMappings = dto.MemberMappings.Select(m => { m.IsCurrentUser = false; m.PersonId = PepeId; return m; }).ToList();

            var act = () => _sut.ConfirmAsync(UserId, EventId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*soy yo*");
        }

        [Fact]
        public async Task ConfirmAsync_UnmappedMember_Throws()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            var dto = BuildConfirmDto();
            dto.MemberMappings = new List<SharedEventImportMemberMappingDTO> { new() { MemberName = "Yo", IsCurrentUser = true } }; // falta "Pepe"

            var act = () => _sut.ConfirmAsync(UserId, EventId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Pepe*");
        }

        [Fact]
        public async Task ConfirmAsync_NewPersonMapping_CreatesPersonAndAddsAsParticipant()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(Comida.Id)).ReturnsAsync(Comida);
            _sharedEventRepoMock.Setup(r => r.GetParticipantAsync(EventId, 42)).ReturnsAsync((SharedEventParticipant?)null);
            _personRepoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Person>()))
                .ReturnsAsync((Person p) => { p.Id = 42; return p; });

            var dto = BuildConfirmDto();
            dto.MemberMappings = new List<SharedEventImportMemberMappingDTO>
            {
                new() { MemberName = "Yo", IsCurrentUser = true },
                new() { MemberName = "Pepe", NewPersonName = "Pepe Nuevo" }
            };

            await _sut.ConfirmAsync(UserId, EventId, dto);

            _personRepoMock.Verify(r => r.AddAsyncReturnObject(It.Is<Person>(p => p.Name == "Pepe Nuevo" && p.UserId == UserId)), Times.Once);
            _sharedEventRepoMock.Verify(r => r.AddParticipantAsync(It.Is<SharedEventParticipant>(p => p.SharedEventId == EventId && p.PersonId == 42)), Times.Once);
        }

        [Fact]
        public async Task ConfirmAsync_ExpenseRowPaidByUser_CreatesMovementWithSharesForBothMembers()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(Comida.Id)).ReturnsAsync(Comida);
            _sharedEventRepoMock.Setup(r => r.GetParticipantAsync(EventId, PepeId)).ReturnsAsync(new SharedEventParticipant { SharedEventId = EventId, PersonId = PepeId });
            _personRepoMock.Setup(r => r.GetByIdAsync(PepeId)).ReturnsAsync(new Person { Id = PepeId, UserId = UserId, Name = "Pepe" });

            // saltear la fila de pago para aislar el caso del movimiento
            var dto = BuildConfirmDto(rowDecisions: new List<SharedEventImportRowDecisionDTO>
            {
                new() { RowIndex = 1, Action = SharedEventImportRowAction.CreateNew, AccountId = 2 },
                new() { RowIndex = 2, Action = SharedEventImportRowAction.Skip }
            });

            SharedEventMovementAddDTO? captured = null;
            _sharedEventServiceMock.Setup(s => s.CreateMovementAsync(UserId, EventId, It.IsAny<SharedEventMovementAddDTO>()))
                .Callback<int, int, SharedEventMovementAddDTO>((_, _, d) => captured = d)
                .ReturnsAsync(new SharedEventMovementDTO());

            var result = await _sut.ConfirmAsync(UserId, EventId, dto);

            result.MovementsCreated.Should().Be(1);
            result.Skipped.Should().Be(1);
            captured.Should().NotBeNull();
            captured!.TotalAmount.Should().Be(10000.00m);
            captured.PayerPersonId.Should().BeNull();
            captured.Payment!.AccountId.Should().Be(2);
            captured.Shares.Should().HaveCount(2);
            // Yo pagué (delta +7000): mi propio consumo = Cost - delta = 10000 - 7000 = 3000; el resto (7000) es de Pepe
            captured.Shares.Single(s => s.PersonId == null).Amount.Should().Be(3000.00m);
            captured.Shares.Single(s => s.PersonId == PepeId).Amount.Should().Be(7000.00m);
        }

        [Fact]
        public async Task ConfirmAsync_ExpenseRowPaidByUserWithCard_BuildsCardPaymentInput()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(Comida.Id)).ReturnsAsync(Comida);
            _sharedEventRepoMock.Setup(r => r.GetParticipantAsync(EventId, PepeId)).ReturnsAsync(new SharedEventParticipant { SharedEventId = EventId, PersonId = PepeId });
            _personRepoMock.Setup(r => r.GetByIdAsync(PepeId)).ReturnsAsync(new Person { Id = PepeId, UserId = UserId, Name = "Pepe" });

            var dto = BuildConfirmDto(rowDecisions: new List<SharedEventImportRowDecisionDTO>
            {
                new() { RowIndex = 1, Action = SharedEventImportRowAction.CreateNew, CardId = 7, Installments = 3, FirstInstallment = new DateTime(2026, 2, 1) },
                new() { RowIndex = 2, Action = SharedEventImportRowAction.Skip }
            });

            SharedEventMovementAddDTO? captured = null;
            _sharedEventServiceMock.Setup(s => s.CreateMovementAsync(UserId, EventId, It.IsAny<SharedEventMovementAddDTO>()))
                .Callback<int, int, SharedEventMovementAddDTO>((_, _, d) => captured = d)
                .ReturnsAsync(new SharedEventMovementDTO());

            var result = await _sut.ConfirmAsync(UserId, EventId, dto);

            result.MovementsCreated.Should().Be(1);
            captured.Should().NotBeNull();
            captured!.Payment!.AccountId.Should().BeNull();
            captured.Payment.CardId.Should().Be(7);
            captured.Payment.Installments.Should().Be(3);
            captured.Payment.FirstInstallment.Should().Be(new DateTime(2026, 2, 1));
        }

        [Fact]
        public async Task ConfirmAsync_ExpenseRowPaidByUserWithBothAccountAndCard_RecordsErrorInsteadOfCreating()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(Comida.Id)).ReturnsAsync(Comida);
            _sharedEventRepoMock.Setup(r => r.GetParticipantAsync(EventId, PepeId)).ReturnsAsync(new SharedEventParticipant { SharedEventId = EventId, PersonId = PepeId });
            _personRepoMock.Setup(r => r.GetByIdAsync(PepeId)).ReturnsAsync(new Person { Id = PepeId, UserId = UserId, Name = "Pepe" });

            var dto = BuildConfirmDto(rowDecisions: new List<SharedEventImportRowDecisionDTO>
            {
                new() { RowIndex = 1, Action = SharedEventImportRowAction.CreateNew, AccountId = 2, CardId = 7 },
                new() { RowIndex = 2, Action = SharedEventImportRowAction.Skip }
            });

            var result = await _sut.ConfirmAsync(UserId, EventId, dto);

            result.MovementsCreated.Should().Be(0);
            result.Errors.Should().ContainSingle(e => e.Contains("exactamente una cuenta o una tarjeta"));
            _sharedEventServiceMock.Verify(s => s.CreateMovementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SharedEventMovementAddDTO>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmAsync_PaymentRow_CreatesPaymentFromReceiverToPayer()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(Comida.Id)).ReturnsAsync(Comida);
            _sharedEventRepoMock.Setup(r => r.GetParticipantAsync(EventId, PepeId)).ReturnsAsync(new SharedEventParticipant { SharedEventId = EventId, PersonId = PepeId });
            _personRepoMock.Setup(r => r.GetByIdAsync(PepeId)).ReturnsAsync(new Person { Id = PepeId, UserId = UserId, Name = "Pepe" });

            var dto = BuildConfirmDto(rowDecisions: new List<SharedEventImportRowDecisionDTO>
            {
                new() { RowIndex = 1, Action = SharedEventImportRowAction.Skip },
                new() { RowIndex = 2, Action = SharedEventImportRowAction.CreateNew, AccountId = 3 }
            });

            SharedEventPaymentAddDTO? captured = null;
            _sharedEventPaymentServiceMock.Setup(s => s.CreatePaymentAsync(UserId, EventId, It.IsAny<SharedEventPaymentAddDTO>()))
                .Callback<int, int, SharedEventPaymentAddDTO>((_, _, d) => captured = d)
                .ReturnsAsync(new SharedEventPaymentDTO());

            var result = await _sut.ConfirmAsync(UserId, EventId, dto);

            result.PaymentsCreated.Should().Be(1);
            captured.Should().NotBeNull();
            // "Pepe me devolvió": delta Pepe=+7000 (pagó), delta Yo=-7000 (recibió) => FromPersonId=Pepe, ToPersonId=yo(null)
            captured!.FromPersonId.Should().Be(PepeId);
            captured.ToPersonId.Should().BeNull();
            captured.Amount.Should().Be(7000.00m);
            captured.AccountId.Should().Be(3);
        }

        [Fact]
        public async Task ConfirmAsync_RowWithoutDecision_IsSkippedAndNotProcessed()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(Comida.Id)).ReturnsAsync(Comida);
            _sharedEventRepoMock.Setup(r => r.GetParticipantAsync(EventId, PepeId)).ReturnsAsync(new SharedEventParticipant { SharedEventId = EventId, PersonId = PepeId });
            _personRepoMock.Setup(r => r.GetByIdAsync(PepeId)).ReturnsAsync(new Person { Id = PepeId, UserId = UserId, Name = "Pepe" });

            var dto = BuildConfirmDto(rowDecisions: new List<SharedEventImportRowDecisionDTO>());

            var result = await _sut.ConfirmAsync(UserId, EventId, dto);

            result.Skipped.Should().Be(2);
            result.MovementsCreated.Should().Be(0);
            result.PaymentsCreated.Should().Be(0);
            _sharedEventServiceMock.Verify(s => s.CreateMovementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SharedEventMovementAddDTO>()), Times.Never);
            _sharedEventPaymentServiceMock.Verify(s => s.CreatePaymentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SharedEventPaymentAddDTO>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmAsync_UnderlyingServiceThrows_RecordsErrorInsteadOfPropagating()
        {
            _sharedEventRepoMock.Setup(r => r.GetWithParticipantsAsync(EventId)).ReturnsAsync(BuildOpenEvent());
            _transactionClassRepoMock.Setup(r => r.GetByIdAsync(Comida.Id)).ReturnsAsync(Comida);
            _sharedEventRepoMock.Setup(r => r.GetParticipantAsync(EventId, PepeId)).ReturnsAsync(new SharedEventParticipant { SharedEventId = EventId, PersonId = PepeId });
            _personRepoMock.Setup(r => r.GetByIdAsync(PepeId)).ReturnsAsync(new Person { Id = PepeId, UserId = UserId, Name = "Pepe" });

            _sharedEventServiceMock.Setup(s => s.CreateMovementAsync(UserId, EventId, It.IsAny<SharedEventMovementAddDTO>()))
                .ThrowsAsync(new BusinessRuleException("no hay suficiente saldo"));

            var dto = BuildConfirmDto(rowDecisions: new List<SharedEventImportRowDecisionDTO>
            {
                new() { RowIndex = 1, Action = SharedEventImportRowAction.CreateNew, AccountId = 2 },
                new() { RowIndex = 2, Action = SharedEventImportRowAction.Skip }
            });

            var result = await _sut.ConfirmAsync(UserId, EventId, dto);

            result.MovementsCreated.Should().Be(0);
            result.Skipped.Should().Be(2);
            result.Errors.Should().ContainSingle(e => e.Contains("no hay suficiente saldo"));
        }
    }
}
