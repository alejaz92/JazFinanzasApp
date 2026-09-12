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
    // Fase 21 (Bloque E, Flujo 7 — Compartidos). "Por evento" no se testea acá: reusa
    // SharedEventService.GetByIdAsync tal cual (ya cubierto por SharedEventServiceTests). Este archivo
    // cubre lo nuevo: la evolución mensual del saldo (solo Eventos, sin el pool de SharedExpense
    // sueltas — ver comentario en SharedEventReportDTOs), el ranking de eventos y el historial por
    // persona.
    public class SharedEventReportServiceTests
    {
        private const int UserId = 1;
        private const int PersonId = 7;
        private static readonly Asset UsdAsset = new() { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD" };

        private readonly Mock<ISharedEventRepository> _sharedEventRepoMock = new();
        private readonly Mock<ISharedEventService> _sharedEventServiceMock = new();
        private readonly Mock<IPersonRepository> _personRepoMock = new();
        private readonly SharedEventReportService _sut;

        public SharedEventReportServiceTests()
        {
            _sharedEventServiceMock.Setup(s => s.GetConsolidatedDebtsAsync(UserId))
                .ReturnsAsync(new List<SharedEventConsolidatedDebtDTO>());
            _sharedEventRepoMock.Setup(r => r.GetAllDetailByUserIdAsync(UserId)).ReturnsAsync(new List<SharedEvent>());

            _sut = new SharedEventReportService(
                _sharedEventRepoMock.Object,
                _sharedEventServiceMock.Object,
                _personRepoMock.Object);
        }

        // ── GetGeneralAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetGeneralAsync_ReturnsBalancesFromConsolidatedDebts_AsIs()
        {
            var debts = new List<SharedEventConsolidatedDebtDTO>
            {
                new() { PersonId = PersonId, PersonName = "Renzo", AssetId = 2, AssetName = "Dolar Estadounidense", AssetSymbol = "USD", PendingInFavor = 50m, PendingAgainst = 0m }
            };
            _sharedEventServiceMock.Setup(s => s.GetConsolidatedDebtsAsync(UserId)).ReturnsAsync(debts);

            var result = await _sut.GetGeneralAsync(UserId);

            result.Balances.Should().BeEquivalentTo(debts);
        }

        [Fact]
        public async Task GetGeneralAsync_BalanceEvolution_ComputesMyBalanceAsOfEachMonth_ContributedMinusConsumed()
        {
            var today = DateTime.UtcNow.Date;
            var sharedEvent = new SharedEvent
            {
                Id = 10,
                Name = "Bariloche 2026",
                Movements = new List<SharedEventMovement>
                {
                    // El usuario pagó 100 (PayerPersonId null) y consumió 60 de su parte -- el resto (40)
                    // lo consumió un tercero, así que MyBalance = 100 - 60 = 40 (me deben esos 40).
                    new()
                    {
                        AssetId = 2, Asset = UsdAsset, Date = today, PayerPersonId = null, TotalAmount = 100m,
                        Shares = new List<SharedEventMovementShare>
                        {
                            new() { PersonId = null, Amount = 60m },
                            new() { PersonId = PersonId, Amount = 40m }
                        }
                    }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetAllDetailByUserIdAsync(UserId)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetGeneralAsync(UserId);

            result.BalanceEvolution.Should().ContainSingle();
            var point = result.BalanceEvolution.Single();
            point.AssetId.Should().Be(2);
            point.AssetSymbol.Should().Be("USD");
            point.Month.Should().Be(new DateTime(today.Year, today.Month, 1));
            point.MyBalance.Should().Be(40m);
        }

        [Fact]
        public async Task GetGeneralAsync_EventRanking_OrdersEventsByTotalAmountDescending()
        {
            var cheap = new SharedEvent
            {
                Id = 1,
                Name = "Cena",
                Movements = new List<SharedEventMovement> { new() { AssetId = 2, Asset = UsdAsset, TotalAmount = 50m, Date = DateTime.UtcNow } }
            };
            var expensive = new SharedEvent
            {
                Id = 2,
                Name = "Bariloche 2026",
                Movements = new List<SharedEventMovement> { new() { AssetId = 2, Asset = UsdAsset, TotalAmount = 900m, Date = DateTime.UtcNow } }
            };
            _sharedEventRepoMock.Setup(r => r.GetAllDetailByUserIdAsync(UserId)).ReturnsAsync(new List<SharedEvent> { cheap, expensive });

            var result = await _sut.GetGeneralAsync(UserId);

            result.EventRanking.Should().HaveCount(2);
            result.EventRanking[0].EventId.Should().Be(2);
            result.EventRanking[0].Amounts.Should().ContainSingle(a => a.AssetId == 2 && a.Total == 900m);
            result.EventRanking[1].EventId.Should().Be(1);
        }

        // ── GetByPersonAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetByPersonAsync_PersonNotFound_ThrowsNotFoundException()
        {
            _personRepoMock.Setup(r => r.GetByIdAsync(PersonId)).ReturnsAsync((Person?)null);

            await FluentActions.Invoking(() => _sut.GetByPersonAsync(UserId, PersonId))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetByPersonAsync_PersonOfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _personRepoMock.Setup(r => r.GetByIdAsync(PersonId)).ReturnsAsync(new Person { Id = PersonId, UserId = 999, Name = "Renzo" });

            await FluentActions.Invoking(() => _sut.GetByPersonAsync(UserId, PersonId))
                .Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task GetByPersonAsync_FiltersConsolidatedBalancesToThisPersonOnly()
        {
            _personRepoMock.Setup(r => r.GetByIdAsync(PersonId)).ReturnsAsync(new Person { Id = PersonId, UserId = UserId, Name = "Renzo" });
            var debts = new List<SharedEventConsolidatedDebtDTO>
            {
                new() { PersonId = PersonId, PersonName = "Renzo", AssetId = 2, AssetSymbol = "USD", PendingInFavor = 40m },
                new() { PersonId = 999, PersonName = "Otra", AssetId = 2, AssetSymbol = "USD", PendingInFavor = 10m }
            };
            _sharedEventServiceMock.Setup(s => s.GetConsolidatedDebtsAsync(UserId)).ReturnsAsync(debts);

            var result = await _sut.GetByPersonAsync(UserId, PersonId);

            result.Balances.Should().ContainSingle(b => b.PersonId == PersonId);
        }

        [Fact]
        public async Task GetByPersonAsync_CategoryTotals_SumsThePersonsShare_NotTheMovementTotal()
        {
            _personRepoMock.Setup(r => r.GetByIdAsync(PersonId)).ReturnsAsync(new Person { Id = PersonId, UserId = UserId, Name = "Renzo" });
            var comida = new TransactionClass { Description = "Comida" };
            var sharedEvent = new SharedEvent
            {
                Id = 10,
                Name = "Bariloche 2026",
                Movements = new List<SharedEventMovement>
                {
                    new()
                    {
                        AssetId = 2, Asset = UsdAsset, Date = DateTime.UtcNow, TransactionClassId = 1, TransactionClass = comida,
                        TotalAmount = 100m,
                        Shares = new List<SharedEventMovementShare>
                        {
                            new() { PersonId = null, Amount = 60m },
                            new() { PersonId = PersonId, Amount = 40m }
                        }
                    }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetAllDetailByUserIdAsync(UserId)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetByPersonAsync(UserId, PersonId);

            result.CategoryTotals.Should().ContainSingle(c => c.TransactionClassName == "Comida" && c.Total == 40m);
        }

        [Fact]
        public async Task GetByPersonAsync_Movements_OnlyIncludesMovementsInvolvingThePerson_WrappedWithEventInfo()
        {
            _personRepoMock.Setup(r => r.GetByIdAsync(PersonId)).ReturnsAsync(new Person { Id = PersonId, UserId = UserId, Name = "Renzo" });
            var involving = new SharedEventMovement
            {
                Id = 1, AssetId = 2, Asset = UsdAsset, Date = DateTime.UtcNow, Description = "Con Renzo",
                Shares = new List<SharedEventMovementShare> { new() { PersonId = PersonId, Amount = 40m } }
            };
            var notInvolving = new SharedEventMovement
            {
                Id = 2, AssetId = 2, Asset = UsdAsset, Date = DateTime.UtcNow, Description = "Sin Renzo",
                Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 100m } }
            };
            var sharedEvent = new SharedEvent { Id = 10, Name = "Bariloche 2026", Movements = new List<SharedEventMovement> { involving, notInvolving } };
            _sharedEventRepoMock.Setup(r => r.GetAllDetailByUserIdAsync(UserId)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetByPersonAsync(UserId, PersonId);

            result.Movements.Should().ContainSingle();
            result.Movements[0].EventId.Should().Be(10);
            result.Movements[0].EventName.Should().Be("Bariloche 2026");
            result.Movements[0].Movement.Description.Should().Be("Con Renzo");
        }

        [Fact]
        public async Task GetByPersonAsync_BalanceEvolution_SignIsFlippedRelativeToGeneral()
        {
            _personRepoMock.Setup(r => r.GetByIdAsync(PersonId)).ReturnsAsync(new Person { Id = PersonId, UserId = UserId, Name = "Renzo" });
            var today = DateTime.UtcNow.Date;

            // Renzo pagó los 100 y el usuario consumió los 100 -- el usuario le debe a Renzo, así que
            // en la convención de "PendingInFavor - PendingAgainst" (positivo = me deben) el punto
            // tiene que dar negativo, aunque el neto de Renzo dentro del evento (ComputeBalances) sea
            // +100 (puso más de lo que consumió). Por eso GetByPersonAsync invierte el signo.
            var sharedEvent = new SharedEvent
            {
                Id = 10,
                Name = "Bariloche 2026",
                Movements = new List<SharedEventMovement>
                {
                    new()
                    {
                        AssetId = 2, Asset = UsdAsset, Date = today, PayerPersonId = PersonId, TotalAmount = 100m,
                        Shares = new List<SharedEventMovementShare> { new() { PersonId = null, Amount = 100m } }
                    }
                }
            };
            _sharedEventRepoMock.Setup(r => r.GetAllDetailByUserIdAsync(UserId)).ReturnsAsync(new List<SharedEvent> { sharedEvent });

            var result = await _sut.GetByPersonAsync(UserId, PersonId);

            result.BalanceEvolution.Should().ContainSingle();
            result.BalanceEvolution.Single().MyBalance.Should().Be(-100m);
        }
    }
}
