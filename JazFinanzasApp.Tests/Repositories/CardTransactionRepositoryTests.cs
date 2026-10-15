using FluentAssertions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.Tests.Repositories
{
    // Presencia mensual de los recurrentes en el resumen del mes, el formulario de pago y la lista de
    // pendientes (docs/plans/activos/plan-devengado-recurrentes.md, Fase 2, D5 y D6). Un tramo "CLOSED"
    // (recurrente al que se le cambió el precio o se dio de baja) cobra en cada mes de su rango.
    public class CardTransactionRepositoryTests
    {
        private const int UserId = 1;
        private const int CardId = 10;

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static void AddCardTransaction(ApplicationDbContext context, int id, string repeat, DateTime firstInstallment, DateTime? lastInstallment, string detail = "Internet")
        {
            if (!context.Cards.Local.Any(c => c.Id == CardId))
            {
                context.Cards.Add(new Card { Id = CardId, Name = "Visa", UserId = UserId });
                context.TransactionClasses.Add(new TransactionClass { Id = 1, Description = "Servicios", IncExp = "E", UserId = UserId });
                context.Assets.Add(new Asset { Id = 1, Name = "Peso Argentino", Symbol = "ARS", Color = "#000000", AssetType = new AssetType { Name = "Moneda", Environment = "FIAT" } });
            }

            context.CardTransactions.Add(new CardTransaction
            {
                Id = id,
                Date = firstInstallment,
                Detail = detail,
                CardId = CardId,
                TransactionClassId = 1,
                AssetId = 1,
                TotalAmount = 1000m,
                InstallmentAmount = 1000m,
                Installments = 1,
                FirstInstallment = firstInstallment,
                LastInstallment = lastInstallment,
                Repeat = repeat,
                UserId = UserId
            });
        }

        private static async Task<List<int>> IdsToPay(ApplicationDbContext context, DateTime month)
        {
            var repository = new CardTransactionRepository(context);
            return (await repository.GetCardTransactionsToPay(CardId, month, UserId)).Select(ct => ct.Id).OrderBy(id => id).ToList();
        }

        // ── GetCardTransactionsToPay ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetCardTransactionsToPay_ClosedTranche_AppearsInEveryMonthOfItsRange_AndNotOutside()
        {
            using var context = CreateContext();
            AddCardTransaction(context, 1, "CLOSED", new DateTime(2026, 2, 1), new DateTime(2026, 4, 1));
            await context.SaveChangesAsync();

            (await IdsToPay(context, new DateTime(2026, 1, 1))).Should().BeEmpty();
            (await IdsToPay(context, new DateTime(2026, 2, 1))).Should().Equal(1);
            (await IdsToPay(context, new DateTime(2026, 3, 15))).Should().Equal(1);
            (await IdsToPay(context, new DateTime(2026, 4, 1))).Should().Equal(1);
            (await IdsToPay(context, new DateTime(2026, 5, 1))).Should().BeEmpty();
        }

        [Fact]
        public async Task GetCardTransactionsToPay_ClosedTrancheWithoutLastInstallment_AppearsOnlyInItsFirstMonth()
        {
            using var context = CreateContext();
            AddCardTransaction(context, 1, "CLOSED", new DateTime(2026, 2, 1), null);
            await context.SaveChangesAsync();

            (await IdsToPay(context, new DateTime(2026, 2, 1))).Should().Equal(1);
            (await IdsToPay(context, new DateTime(2026, 3, 1))).Should().BeEmpty();
        }

        [Fact]
        public async Task GetCardTransactionsToPay_ActiveRecurrentAndInstallmentPurchase_KeepWorkingAsBefore()
        {
            using var context = CreateContext();
            AddCardTransaction(context, 1, "YES", new DateTime(2026, 2, 1), new DateTime(1, 1, 1));   // vigente con 0001-01-01
            AddCardTransaction(context, 2, "YES", new DateTime(2026, 2, 1), null);
            AddCardTransaction(context, 3, "NO", new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)); // 2 cuotas
            await context.SaveChangesAsync();

            (await IdsToPay(context, new DateTime(2026, 1, 1))).Should().BeEmpty();
            (await IdsToPay(context, new DateTime(2026, 3, 1))).Should().Equal(1, 2, 3);
            (await IdsToPay(context, new DateTime(2026, 9, 1))).Should().Equal(1, 2);
        }

        // ── GetPendingCardTransactionsAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetPendingCardTransactionsAsync_ClosedTrancheEndingInTheFuture_IsPendingAndNotEditable()
        {
            using var context = CreateContext();
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            AddCardTransaction(context, 1, "CLOSED", currentMonth.AddMonths(-3), currentMonth.AddMonths(2), detail: "Baja futura");
            AddCardTransaction(context, 2, "CLOSED", currentMonth.AddMonths(-6), currentMonth.AddMonths(-1), detail: "Ya terminado");
            await context.SaveChangesAsync();

            var pending = (await new CardTransactionRepository(context).GetPendingCardTransactionsAsync(UserId)).ToList();

            pending.Should().ContainSingle();
            pending.Single().Id.Should().Be(1);
            pending.Single().Installments.Should().Be("Recurrente (con baja)");
            pending.Single().LastInstallment.Should().Be(currentMonth.AddMonths(2).ToString("MM/yyyy"));
        }

        [Fact]
        public async Task GetPendingCardTransactionsAsync_ActiveRecurrent_StillShowsAsEditableRecurrent()
        {
            using var context = CreateContext();
            AddCardTransaction(context, 1, "YES", new DateTime(2026, 2, 1), new DateTime(1, 1, 1));
            await context.SaveChangesAsync();

            var pending = (await new CardTransactionRepository(context).GetPendingCardTransactionsAsync(UserId)).ToList();

            pending.Should().ContainSingle();
            pending.Single().Installments.Should().Be("Recurrente");
            pending.Single().LastInstallment.Should().Be("NA");
        }
    }
}
