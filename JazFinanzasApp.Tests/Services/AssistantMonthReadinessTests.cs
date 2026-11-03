using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.DTO.BondCollection;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm-v2.md, Fase 5 — "mes listo" (D4) y datos faltantes (D5).
    public class AssistantMonthReadinessDecisionTests
    {
        private static readonly DateTime September = new(2026, 9, 1);

        [Fact]
        public void WithNothingMissingTheMonthIsReadyAtAnyDay()
        {
            var result = AssistantMonthReadiness.Decide(Array.Empty<string>(), September, new DateTime(2026, 10, 2));

            result.Ready.Should().BeTrue();
            result.ShouldPrepare.Should().BeTrue();
        }

        [Fact]
        public void WithSomethingMissingItWaitsUntilTheDeadline()
        {
            var result = AssistantMonthReadiness.Decide(new[] { "falta" }, September, new DateTime(2026, 10, 6));

            result.Ready.Should().BeFalse();
            result.DeadlineReached.Should().BeFalse();
            result.ShouldPrepare.Should().BeFalse();
        }

        [Fact]
        public void OnTheSeventhItIsPreparedAnywayAndKeepsWhatWasMissing()
        {
            var result = AssistantMonthReadiness.Decide(new[] { "falta" }, September, new DateTime(2026, 10, 7));

            result.Ready.Should().BeFalse();
            result.DeadlineReached.Should().BeTrue();
            result.ShouldPrepare.Should().BeTrue();
            result.Missing.Should().Equal("falta");
        }

        [Fact]
        public void TheDeadlineIsCountedFromTheMonthThatClosedAcrossTheYear()
        {
            AssistantMonthReadiness.Decide(new[] { "falta" }, new DateTime(2026, 12, 15), new DateTime(2027, 1, 6)).ShouldPrepare.Should().BeFalse();
            AssistantMonthReadiness.Decide(new[] { "falta" }, new DateTime(2026, 12, 15), new DateTime(2027, 1, 7)).ShouldPrepare.Should().BeTrue();
        }

        [Fact]
        public void TheMissingTextsNameTheAssetsAndTheBonds()
        {
            var bond = new BondCollectionPendingDTO { Symbol = "AL30", PaymentDate = new DateTime(2026, 9, 9) };

            var missing = AssistantMonthReadinessService.BuildMissing(new[] { "GGAL", "AAPL" }, new[] { bond }, new DateTime(2026, 9, 30));

            missing.Should().HaveCount(2);
            missing[0].Should().Contain("30/09").And.Contain("GGAL, AAPL");
            missing[1].Should().Contain("AL30").And.Contain("09/09");
        }

        [Fact]
        public void WithNothingToReportThereAreNoMissingTexts()
        {
            AssistantMonthReadinessService.BuildMissing(Array.Empty<string>(), Array.Empty<BondCollectionPendingDTO>(), new DateTime(2026, 9, 30)).Should().BeEmpty();
        }
    }

    public class AssistantMonthReadinessServiceTests
    {
        private const int UserId = 1;

        [Fact]
        public async Task ItAsksForTheQuotesOfTheLastDayOfTheMonthAndKeepsOnlyTheBondsOfThatMonth()
        {
            var transactions = new Mock<ITransactionRepository>();
            transactions.Setup(t => t.GetHeldAssetsWithoutQuoteAsync(UserId, new DateTime(2026, 9, 30)))
                .ReturnsAsync(new[] { new StaleAssetResult { AssetSymbol = "GGAL", QuoteDate = new DateTime(2026, 9, 28) } });
            var bonds = new Mock<IBondCollectionService>();
            bonds.Setup(b => b.GetPendingAsync(UserId)).ReturnsAsync(new[]
            {
                new BondCollectionPendingDTO { Symbol = "AL30", PaymentDate = new DateTime(2026, 9, 9) },
                new BondCollectionPendingDTO { Symbol = "GD30", PaymentDate = new DateTime(2026, 10, 9) }, // de otro mes
                new BondCollectionPendingDTO { Symbol = "BPOC7", PaymentDate = new DateTime(2026, 8, 31) } // de otro mes
            });
            var sut = new AssistantMonthReadinessService(transactions.Object, bonds.Object);

            var result = await sut.CheckAsync(UserId, new DateTime(2026, 9, 12), new DateTime(2026, 10, 3));

            result.Ready.Should().BeFalse();
            result.Missing.Should().HaveCount(2);
            result.Missing[1].Should().Contain("AL30").And.NotContain("GD30").And.NotContain("BPOC7");
            result.ShouldPrepare.Should().BeFalse();
        }

        [Fact]
        public async Task WithNothingMissingTheMonthIsReady()
        {
            var transactions = new Mock<ITransactionRepository>();
            transactions.Setup(t => t.GetHeldAssetsWithoutQuoteAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Array.Empty<StaleAssetResult>());
            var bonds = new Mock<IBondCollectionService>();
            bonds.Setup(b => b.GetPendingAsync(UserId)).ReturnsAsync(Array.Empty<BondCollectionPendingDTO>());

            var result = await new AssistantMonthReadinessService(transactions.Object, bonds.Object).CheckAsync(UserId, new DateTime(2026, 9, 1), new DateTime(2026, 10, 2));

            result.Ready.Should().BeTrue();
            result.Missing.Should().BeEmpty();
        }
    }

    public class AssistantHeldAssetsWithoutQuoteRepositoryTests
    {
        private const int UserId = 1;
        private static readonly DateTime LastDay = new(2026, 9, 30);

        private static ApplicationDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static Transaction Tx(Asset asset, DateTime date, decimal amount, int userId = UserId) => new()
        {
            UserId = userId, Asset = asset, AccountId = 1, PortfolioId = 1, Date = date, MovementType = "C", Amount = amount, QuotePrice = 1m
        };

        private static Asset NewAsset(AssetType type, string symbol) => new() { Name = symbol + " nombre", Symbol = symbol, Color = "#000000", AssetType = type };

        private static AssetQuote Quote(Asset asset, DateTime date, string type = "NA") => new() { Asset = asset, Date = date, Type = type, Value = 10m };

        [Fact]
        public async Task ListsWhatWasHeldAtMonthEndWithoutAQuoteOfThatDayOrLater_AndIgnoresTheRest()
        {
            using var context = CreateContext();
            var type = new AssetType { Name = "Acciones", Environment = "STOCK" };
            var behind = NewAsset(type, "GGAL");      // en cartera, última cotización del 28/09
            var upToDate = NewAsset(type, "AAPL");    // en cartera, con cotización del 30/09
            var later = NewAsset(type, "MELI");       // en cartera, con cotización del 02/10 (posterior)
            var sold = NewAsset(type, "TSLA");        // vendido antes de fin de mes
            var boughtAfter = NewAsset(type, "NVDA"); // comprado en octubre
            var otherUser = NewAsset(type, "KO");     // de otro usuario
            var cardRate = NewAsset(type, "XYZ");     // solo cotización TARJETA: no es una cotización del activo
            context.Accounts.Add(new Account { Id = 1, Name = "Cuenta", UserId = UserId });
            context.Transactions.AddRange(
                Tx(behind, new DateTime(2026, 8, 1), 10m), Tx(upToDate, new DateTime(2026, 8, 1), 10m), Tx(later, new DateTime(2026, 8, 1), 10m),
                Tx(sold, new DateTime(2026, 8, 1), 10m), Tx(sold, new DateTime(2026, 9, 15), -10m),
                Tx(boughtAfter, new DateTime(2026, 10, 2), 10m),
                Tx(otherUser, new DateTime(2026, 8, 1), 10m, userId: 2),
                Tx(cardRate, new DateTime(2026, 8, 1), 10m));
            context.AssetQuotes.AddRange(
                Quote(behind, new DateTime(2026, 9, 28)), Quote(upToDate, LastDay), Quote(later, new DateTime(2026, 10, 2)),
                Quote(sold, new DateTime(2026, 9, 1)), Quote(boughtAfter, new DateTime(2026, 9, 1)), Quote(otherUser, new DateTime(2026, 9, 1)),
                Quote(cardRate, new DateTime(2026, 9, 1), type: "TARJETA"));
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetHeldAssetsWithoutQuoteAsync(UserId, LastDay)).ToList();

            result.Select(r => r.AssetSymbol).Should().Equal("GGAL");
            result[0].QuoteDate.Should().Be(new DateTime(2026, 9, 28));
        }
    }

    public class AssistantContextDataNotesTests
    {
        private static AssistantContextRequest CloseRequest() =>
            new(1, AssistantReviewKind.MonthClose, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

        private static Mock<IAssistantMonthReadinessService> Readiness(params string[] missing)
        {
            var mock = new Mock<IAssistantMonthReadinessService>();
            mock.Setup(r => r.CheckAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new AssistantMonthReadiness(missing.Length == 0, missing, false));
            return mock;
        }

        [Fact]
        public async Task ACloseCarriesWhatWasMissing()
        {
            var builder = new AssistantContextBuilder(Array.Empty<IAssistantContextSource>(), Readiness("falta A", "falta B").Object);

            var context = await builder.BuildAsync(CloseRequest());

            context.DataNotes.Should().Equal("falta A", "falta B");
            AssistantContextBuilder.Serialize(context).Should().Contain("\"dataNotes\":[\"falta A\",\"falta B\"]");
        }

        [Fact]
        public async Task ACompleteCloseHasNoDataNotesSection()
        {
            var builder = new AssistantContextBuilder(Array.Empty<IAssistantContextSource>(), Readiness().Object);

            var context = await builder.BuildAsync(CloseRequest());

            context.DataNotes.Should().BeNull();
            AssistantContextBuilder.Serialize(context).Should().NotContain("dataNotes");
        }

        [Fact]
        public async Task TheMidMonthDoesNotWaitForOrCheckAnything()
        {
            var readiness = Readiness("falta A");
            var builder = new AssistantContextBuilder(Array.Empty<IAssistantContextSource>(), readiness.Object);

            var context = await builder.BuildAsync(new AssistantContextRequest(1, AssistantReviewKind.MidMonth, new DateTime(2026, 10, 1), new DateTime(2026, 10, 16)));

            context.DataNotes.Should().BeNull();
            readiness.Verify(r => r.CheckAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task TheGeneratedReviewStoresAndExposesTheDataNotes()
        {
            var f = new AssistantReviewServiceTests.Fixture(readiness: Readiness("Falta la cotización del 30/09 de: GGAL.").Object);
            f.AddActivityInSeptember();

            var dto = await f.Service.GenerateAsync(1, AssistantReviewServiceTests.Close());

            dto.DataNotes.Should().Equal("Falta la cotización del 30/09 de: GGAL.");
            f.Context.AssistantReviews.Single().DataNotesJson.Should().Contain("GGAL");
        }
    }
}
