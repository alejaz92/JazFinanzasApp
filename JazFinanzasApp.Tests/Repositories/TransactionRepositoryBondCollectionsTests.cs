using FluentAssertions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.Tests.Repositories
{
    // plan-amortizaciones-bonos, Fase 8 (T10/T11): lo cobrado de un bono (capital + interés de sus
    // BondCollection Registered/Untracked) se suma al resultado de la tenencia, sin tocar el valor
    // original ni el actual. Dismissed y los cobros de otro usuario no cuentan.
    public class TransactionRepositoryBondCollectionsTests
    {
        private const int UserId = 1;

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static (ApplicationDbContext Context, Asset Reference, Asset Bond) CreateScenario()
        {
            var context = CreateContext();

            var currencyType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var reference = new Asset { Name = "Dolar Estadounidense", Symbol = "USD", Color = "#000000", AssetType = currencyType };
            var bondType = new AssetType { Name = "Bono", Environment = "BOLSA" };
            var bond = new Asset { Name = "Bono AL30", Symbol = "AL30", Color = "#000000", AssetType = bondType };
            context.AssetTypes.AddRange(currencyType, bondType);
            context.Assets.AddRange(reference, bond);

            context.Accounts.Add(new Account { Id = 1, Name = "Bull Market", UserId = UserId });
            context.Accounts.Add(new Account { Id = 2, Name = "Otro broker", UserId = UserId });
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Default", UserId = UserId, IsDefault = true });
            context.Portfolios.Add(new Portfolio { Id = 2, Name = "Largo plazo", UserId = UserId });

            return (context, reference, bond);
        }

        private static void AddBuy(ApplicationDbContext context, Asset asset, DateTime date, decimal amount, decimal quotePrice, int accountId = 1, int portfolioId = 1)
        {
            context.Transactions.Add(new Transaction
            {
                UserId = UserId,
                Asset = asset,
                AccountId = accountId,
                PortfolioId = portfolioId,
                Date = date,
                MovementType = amount >= 0 ? "I" : "E",
                Amount = amount,
                QuotePrice = quotePrice
            });
        }

        private static BondPayment AddPayment(ApplicationDbContext context, Asset bond, Asset currency, DateTime paymentDate) =>
            context.BondPayments.Add(new BondPayment
            {
                Asset = bond,
                PaymentDate = paymentDate,
                InterestPer100 = 1m,
                AmortizationRate = 0.04m,
                CurrencyAsset = currency
            }).Entity;

        private static void AddCollection(ApplicationDbContext context, BondPayment payment, string status, decimal capital, decimal interest,
            DateTime collectionDate, int userId = UserId, int accountId = 1, int portfolioId = 1, decimal? quotePrice = 1m)
        {
            context.BondCollections.Add(new BondCollection
            {
                BondPayment = payment,
                UserId = userId,
                AccountId = accountId,
                PortfolioId = portfolioId,
                Status = status,
                HeldQuantity = 5m,
                CollectionDate = collectionDate,
                CapitalAmount = capital,
                InterestAmount = interest,
                QuotePrice = quotePrice
            });
        }

        // Compra de 5 unidades a USD 80 (costo 400), cotización actual USD 60 (valor 300).
        private static void SeedBondPosition(ApplicationDbContext context, Asset reference, Asset bond)
        {
            var purchaseDate = new DateTime(2026, 1, 1);
            AddBuy(context, bond, purchaseDate, amount: 5m, quotePrice: 1m / 80m);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = purchaseDate, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = new DateTime(2026, 6, 1), Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = bond, Date = new DateTime(2026, 6, 1), Type = "NA", Value = 1m / 60m });
        }

        [Fact]
        public async Task GetInvestmentHoldingsAsync_BondWithCollections_AddsCollectedValueWithoutChangingOriginalNorActual()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;
            SeedBondPosition(context, reference, bond);
            var march = AddPayment(context, bond, reference, new DateTime(2026, 3, 9));
            var april = AddPayment(context, bond, reference, new DateTime(2026, 4, 9));
            AddCollection(context, march, BondCollectionStatus.Registered, capital: 20m, interest: 5m, collectionDate: new DateTime(2026, 3, 10));
            AddCollection(context, april, BondCollectionStatus.Untracked, capital: 0m, interest: 10m, collectionDate: new DateTime(2026, 4, 9));
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetInvestmentHoldingsAsync(UserId, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].Quantity.Should().Be(5m);
            result[0].OriginalValue.Should().Be(400m);   // 5 x USD 80, igual que sin cobros
            result[0].ActualValue.Should().Be(300m);     // 5 x USD 60, igual que sin cobros
            result[0].CollectedValue.Should().Be(35m);   // (20 + 5) + (0 + 10)
        }

        [Fact]
        public async Task GetInvestmentHoldingsAsync_DismissedAndOtherUsersCollections_AreNotCounted()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;
            SeedBondPosition(context, reference, bond);
            var march = AddPayment(context, bond, reference, new DateTime(2026, 3, 9));
            var april = AddPayment(context, bond, reference, new DateTime(2026, 4, 9));
            var may = AddPayment(context, bond, reference, new DateTime(2026, 5, 9));
            AddCollection(context, march, BondCollectionStatus.Registered, 20m, 5m, new DateTime(2026, 3, 10));
            AddCollection(context, april, BondCollectionStatus.Dismissed, 999m, 999m, new DateTime(2026, 4, 9));
            AddCollection(context, may, BondCollectionStatus.Registered, 777m, 777m, new DateTime(2026, 5, 10), userId: 99);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetInvestmentHoldingsAsync(UserId, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].CollectedValue.Should().Be(25m); // solo el Registered del usuario
        }

        [Fact]
        public async Task GetInvestmentHoldingsAsync_WithoutCollections_HasZeroCollectedValue()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;
            SeedBondPosition(context, reference, bond);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetInvestmentHoldingsAsync(UserId, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].CollectedValue.Should().Be(0m);
            result[0].OriginalValue.Should().Be(400m);
            result[0].ActualValue.Should().Be(300m);
        }

        [Fact]
        public async Task GetInvestmentHoldingsAsync_CollectionWithoutQuotePrice_ContributesZero()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;
            SeedBondPosition(context, reference, bond);
            var march = AddPayment(context, bond, reference, new DateTime(2026, 3, 9));
            AddCollection(context, march, BondCollectionStatus.Registered, 20m, 5m, new DateTime(2026, 3, 10), quotePrice: null);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetInvestmentHoldingsAsync(UserId, reference.Id)).ToList();

            result[0].CollectedValue.Should().Be(0m);
        }

        [Fact]
        public async Task GetPortfolioStatsAsync_SumsCollectedValueOfTheCollectionsPortfolio()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;
            SeedBondPosition(context, reference, bond);
            var march = AddPayment(context, bond, reference, new DateTime(2026, 3, 9));
            AddCollection(context, march, BondCollectionStatus.Registered, 20m, 5m, new DateTime(2026, 3, 10), portfolioId: 1);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetPortfolioStatsAsync(UserId, reference.Id)).ToList();

            result.Single(p => p.PortfolioId == 1).CollectedValue.Should().Be(25m);
            result.Single(p => p.PortfolioId == 2).CollectedValue.Should().Be(0m);
            result.Single(p => p.PortfolioId == 1).OriginalValue.Should().Be(400m);
            result.Single(p => p.PortfolioId == 1).ActualValue.Should().Be(300m);
        }

        [Fact]
        public async Task GetPortfolioHoldingsAsync_AttachesCollectedValueToTheCollectionsAccount()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;
            SeedBondPosition(context, reference, bond);
            // Mismo bono en una segunda cuenta de la misma cartera, sin ningún cobro.
            AddBuy(context, bond, new DateTime(2026, 1, 1), amount: 2m, quotePrice: 1m / 80m, accountId: 2);
            var march = AddPayment(context, bond, reference, new DateTime(2026, 3, 9));
            AddCollection(context, march, BondCollectionStatus.Registered, 20m, 5m, new DateTime(2026, 3, 10), accountId: 1);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetPortfolioHoldingsAsync(UserId, 1, reference.Id)).ToList();

            result.Should().HaveCount(2);
            result.Single(h => h.AccountName == "Bull Market").CollectedValue.Should().Be(25m);
            result.Single(h => h.AccountName == "Otro broker").CollectedValue.Should().Be(0m);
        }

        [Fact]
        public async Task GetClosedInvestmentPositionsAsync_SoldBondWithCollections_AddsCollectedToRealizedResult()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;
            // Compra 5 a USD 80 (costo 400) y vende las 5 a USD 100 (ingreso 500): ganancia realizada 100.
            AddBuy(context, bond, new DateTime(2026, 1, 1), amount: 5m, quotePrice: 1m / 80m);
            AddBuy(context, bond, new DateTime(2026, 5, 1), amount: -5m, quotePrice: 1m / 100m);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = new DateTime(2026, 1, 1), Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = new DateTime(2026, 5, 1), Type = "NA", Value = 1m });
            var march = AddPayment(context, bond, reference, new DateTime(2026, 3, 9));
            AddCollection(context, march, BondCollectionStatus.Registered, 20m, 5m, new DateTime(2026, 3, 10));
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetClosedInvestmentPositionsAsync(UserId, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].CollectedValue.Should().Be(25m);
            result[0].RealizedResult.Should().Be(125m); // 100 de la compra/venta + 25 cobrados
        }

        [Fact]
        public async Task GetClosedInvestmentPositionsAsync_BondStillHeld_IsNotListedAsClosedBecauseOfCollections()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;
            SeedBondPosition(context, reference, bond);
            var march = AddPayment(context, bond, reference, new DateTime(2026, 3, 9));
            AddCollection(context, march, BondCollectionStatus.Registered, 20m, 5m, new DateTime(2026, 3, 10));
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetClosedInvestmentPositionsAsync(UserId, reference.Id)).ToList();

            result.Should().BeEmpty();
        }

        // ── Aportes vs rendimiento (Fase 10, T12) ────────────────────────────────────────────────

        // Un bono comprado en marzo de 2024 (aporte 400) y vendido en parte en abril, con cobros de
        // capital + interés: lo cobrado se suma a los retiros del mes de su acreditación, y el interés
        // se informa aparte. Dismissed y lo anterior a `since` no cuentan.
        [Fact]
        public async Task GetInvestmentContributionsByMonthAsync_BondCollections_AddToWithdrawnAndSeparateTheInterest()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;

            AddBuy(context, bond, new DateTime(2024, 3, 15), amount: 5m, quotePrice: 1m / 80m);   // aporte: 400
            AddBuy(context, bond, new DateTime(2024, 4, 20), amount: -1m, quotePrice: 1m / 100m); // venta: 100
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = new DateTime(2024, 1, 1), Type = "NA", Value = 1m });

            var before = AddPayment(context, bond, reference, new DateTime(2024, 2, 9));
            var april = AddPayment(context, bond, reference, new DateTime(2024, 4, 9));
            var may = AddPayment(context, bond, reference, new DateTime(2024, 5, 9));
            var june = AddPayment(context, bond, reference, new DateTime(2024, 6, 9));
            AddCollection(context, before, BondCollectionStatus.Registered, 500m, 500m, new DateTime(2024, 2, 10));  // antes de `since`
            AddCollection(context, april, BondCollectionStatus.Registered, 20m, 5m, new DateTime(2024, 4, 10));
            AddCollection(context, may, BondCollectionStatus.Untracked, 0m, 10m, new DateTime(2024, 5, 9));
            AddCollection(context, june, BondCollectionStatus.Dismissed, 999m, 999m, new DateTime(2024, 6, 9));
            await context.SaveChangesAsync();

            var months = (await new TransactionRepository(context)
                .GetInvestmentContributionsByMonthAsync(UserId, reference.Id, since: new DateTime(2024, 3, 1))).ToList();

            var mar = months.Single(m => m.Month == new DateTime(2024, 3, 1));
            mar.Contributed.Should().Be(400m);
            mar.Withdrawn.Should().Be(0m);

            var apr = months.Single(m => m.Month == new DateTime(2024, 4, 1));
            apr.Withdrawn.Should().Be(125m);          // 100 de la venta + 25 cobrados (20 de capital + 5 de interés)
            apr.CollectedValue.Should().Be(25m);
            apr.InterestCollected.Should().Be(5m);

            var may2 = months.Single(m => m.Month == new DateTime(2024, 5, 1));
            may2.Withdrawn.Should().Be(10m);
            may2.InterestCollected.Should().Be(10m);

            var jun = months.Single(m => m.Month == new DateTime(2024, 6, 1));
            jun.Withdrawn.Should().Be(0m);            // Dismissed no cuenta
            jun.InterestCollected.Should().Be(0m);

            months.Sum(m => m.Withdrawn).Should().Be(135m);          // 100 + 25 + 10, sin el cobro de febrero
            months.Sum(m => m.InterestCollected).Should().Be(15m);
        }

        [Fact]
        public async Task GetInvestmentContributionsByMonthAsync_WithoutCollections_HasZeroCollectedAndInterest()
        {
            var (context, reference, bond) = CreateScenario();
            using var _ = context;

            AddBuy(context, bond, new DateTime(2024, 3, 15), amount: 5m, quotePrice: 1m / 80m);
            AddBuy(context, bond, new DateTime(2024, 4, 20), amount: -1m, quotePrice: 1m / 100m);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = new DateTime(2024, 1, 1), Type = "NA", Value = 1m });
            await context.SaveChangesAsync();

            var months = (await new TransactionRepository(context)
                .GetInvestmentContributionsByMonthAsync(UserId, reference.Id, since: new DateTime(2024, 3, 1))).ToList();

            months.Sum(m => m.Withdrawn).Should().Be(100m);
            months.Sum(m => m.CollectedValue).Should().Be(0m);
            months.Sum(m => m.InterestCollected).Should().Be(0m);
        }
    }
}
