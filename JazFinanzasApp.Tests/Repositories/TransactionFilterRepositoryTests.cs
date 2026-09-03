using FluentAssertions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.Tests.Repositories
{
    // Fase 13 — drill-down desde los reportes de Ingresos y Egresos: filtros opcionales agregados a
    // GetPaginatedTransactions (classId, tagId, from, to), todos sin efecto cuando se omiten.
    public class TransactionFilterRepositoryTests
    {
        private const int UserId = 1;

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static Asset AddAsset(ApplicationDbContext context)
        {
            var assetType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var asset = new Asset { Name = "Dolar Estadounidense", Symbol = "USD", Color = "#000", AssetType = assetType };
            context.AssetTypes.Add(assetType);
            context.Assets.Add(asset);
            return asset;
        }

        private static Account GetOrAddAccount(ApplicationDbContext context, int accountId)
        {
            var existing = context.Accounts.Local.FirstOrDefault(a => a.Id == accountId) ?? context.Accounts.FirstOrDefault(a => a.Id == accountId);
            if (existing != null) return existing;
            var account = new Account { Id = accountId, Name = $"Cuenta {accountId}", UserId = UserId };
            context.Accounts.Add(account);
            return account;
        }

        private static Portfolio GetOrAddPortfolio(ApplicationDbContext context)
        {
            var existing = context.Portfolios.Local.FirstOrDefault(p => p.UserId == UserId) ?? context.Portfolios.FirstOrDefault(p => p.UserId == UserId);
            if (existing != null) return existing;
            var portfolio = new Portfolio { Name = "Default", UserId = UserId };
            context.Portfolios.Add(portfolio);
            return portfolio;
        }

        // La query original de GetPaginatedTransactions filtra por m.Account.UserId (no por
        // m.UserId) y hace Include de Account/Portfolio/etc., así que el test tiene que setear esas
        // navegaciones (no solo los ids) para que el InMemory provider las resuelva.
        private static Transaction AddTransaction(ApplicationDbContext context, Asset asset, TransactionClass txClass, DateTime date, decimal amount, int accountId = 1)
        {
            var account = GetOrAddAccount(context, accountId);
            var portfolio = GetOrAddPortfolio(context);
            var t = new Transaction
            {
                UserId = UserId,
                Asset = asset,
                Account = account,
                Portfolio = portfolio,
                Date = date,
                MovementType = "E",
                Amount = -Math.Abs(amount),
                QuotePrice = 1m,
                TransactionClass = txClass
            };
            context.Transactions.Add(t);
            return t;
        }

        [Fact]
        public async Task GetPaginatedTransactions_NoFilters_BehavesExactlyAsBefore()
        {
            using var context = CreateContext();
            var asset = AddAsset(context);
            var super = new TransactionClass { Description = "Supermercado", IncExp = "E", UserId = UserId };
            var combustible = new TransactionClass { Description = "Combustible", IncExp = "E", UserId = UserId };
            context.TransactionClasses.AddRange(super, combustible);

            AddTransaction(context, asset, super, new DateTime(2026, 8, 1), 100m);
            AddTransaction(context, asset, combustible, new DateTime(2026, 8, 2), 50m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var (transactions, totalCount) = await repo.GetPaginatedTransactions(UserId, 1, 20);

            totalCount.Should().Be(2);
            transactions.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPaginatedTransactions_FilterByClassId_ReturnsOnlyThatCategory()
        {
            using var context = CreateContext();
            var asset = AddAsset(context);
            var super = new TransactionClass { Description = "Supermercado", IncExp = "E", UserId = UserId };
            var combustible = new TransactionClass { Description = "Combustible", IncExp = "E", UserId = UserId };
            context.TransactionClasses.AddRange(super, combustible);

            AddTransaction(context, asset, super, new DateTime(2026, 8, 1), 100m);
            AddTransaction(context, asset, combustible, new DateTime(2026, 8, 2), 50m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var (transactions, totalCount) = await repo.GetPaginatedTransactions(UserId, 1, 20, classId: super.Id);

            totalCount.Should().Be(1);
            transactions.Should().ContainSingle(t => t.TransactionClassId == super.Id);
        }

        [Fact]
        public async Task GetPaginatedTransactions_FilterByDateRange_ExcludesOutsideRange()
        {
            using var context = CreateContext();
            var asset = AddAsset(context);
            var super = new TransactionClass { Description = "Supermercado", IncExp = "E", UserId = UserId };
            context.TransactionClasses.Add(super);

            AddTransaction(context, asset, super, new DateTime(2026, 7, 15), 100m);
            AddTransaction(context, asset, super, new DateTime(2026, 8, 15), 200m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var (transactions, totalCount) = await repo.GetPaginatedTransactions(UserId, 1, 20,
                from: new DateTime(2026, 8, 1), to: new DateTime(2026, 9, 1));

            totalCount.Should().Be(1);
            transactions.Single().Date.Should().Be(new DateTime(2026, 8, 15));
        }

        [Fact]
        public async Task GetPaginatedTransactions_FilterByTagId_ReturnsOnlyTaggedTransactions()
        {
            using var context = CreateContext();
            var asset = AddAsset(context);
            var super = new TransactionClass { Description = "Supermercado", IncExp = "E", UserId = UserId };
            context.TransactionClasses.Add(super);

            var tagged = AddTransaction(context, asset, super, new DateTime(2026, 8, 1), 100m);
            AddTransaction(context, asset, super, new DateTime(2026, 8, 2), 50m); // sin etiquetar
            var tag = new Tag { Name = "Auto", UserId = UserId };
            context.Tags.Add(tag);
            await context.SaveChangesAsync();

            context.TransactionTags.Add(new TransactionTag { Transaction = tagged, Tag = tag });
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var (transactions, totalCount) = await repo.GetPaginatedTransactions(UserId, 1, 20, tagId: tag.Id);

            totalCount.Should().Be(1);
            transactions.Single().Id.Should().Be(tagged.Id);
        }
    }
}
