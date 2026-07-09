using FluentAssertions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.Tests.Repositories
{
    // Cubre GetStockStatsAsync tras reemplazar el stored procedure [dbo].[GetStockStats] por LINQ
    // (docs/plans/activos/reemplazar-stored-procedures.md, Fase 1). Los valores esperados replican
    // a mano la fórmula del SP original: QuotePrice/AssetQuote.Value se guardan como 1/precio, según
    // la convención ya documentada en GetAverageQuotePrice.
    public class TransactionRepositoryTests
    {
        private const int UserId = 1;

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static Asset AddReferenceAsset(ApplicationDbContext context, string name = "Dolar Estadounidense")
        {
            var assetType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var asset = new Asset { Name = name, Symbol = "USD", Color = "#000000", AssetType = assetType };
            context.AssetTypes.Add(assetType);
            context.Assets.Add(asset);
            return asset;
        }

        private static Asset AddInvestmentAsset(ApplicationDbContext context, string name, string symbol, string environment, string assetTypeName = "Accion USA")
        {
            var assetType = new AssetType { Name = assetTypeName, Environment = environment };
            var asset = new Asset { Name = name, Symbol = symbol, Color = "#000000", AssetType = assetType };
            context.AssetTypes.Add(assetType);
            context.Assets.Add(asset);
            return asset;
        }

        private static Transaction AddTransaction(ApplicationDbContext context, Asset asset, DateTime date, decimal amount, decimal quotePrice)
        {
            var transaction = new Transaction
            {
                UserId = UserId,
                Asset = asset,
                AccountId = 1,
                PortfolioId = 1,
                Date = date,
                MovementType = amount >= 0 ? "I" : "E",
                Amount = amount,
                QuotePrice = quotePrice
            };
            context.Transactions.Add(transaction);
            return transaction;
        }

        [Fact]
        public async Task GetStockStatsAsync_SingleTransaction_CalculatesOriginalAndActualValue()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var stock = AddInvestmentAsset(context, "Apple", "AAPL", "BOLSA");

            var purchaseDate = new DateTime(2026, 1, 1);
            AddTransaction(context, stock, purchaseDate, amount: 10m, quotePrice: 1m / 100m); // compradas a $100 c/u

            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = purchaseDate, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = new DateTime(2026, 6, 1), Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = stock, Date = new DateTime(2026, 6, 1), Type = "NA", Value = 1m / 150m }); // última cotización: $150

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetStockStatsAsync(UserId, 0, "BOLSA", true, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].AssetName.Should().Be("Apple");
            result[0].Quantity.Should().Be(10m);
            result[0].OriginalValue.Should().Be(1000m); // 10 * $100
            result[0].ActualValue.Should().Be(1500m);   // 10 * $150
        }

        [Fact]
        public async Task GetStockStatsAsync_WithSplitAfterPurchase_AdjustsQuantityAndActualValueButNotOriginalValue()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var stock = AddInvestmentAsset(context, "Apple", "AAPL", "BOLSA");

            var purchaseDate = new DateTime(2026, 1, 1);
            AddTransaction(context, stock, purchaseDate, amount: 10m, quotePrice: 1m / 100m);

            // split 2:1 después de la compra
            context.AssetSplitEvents.Add(new AssetSplitEvent { AssetId = stock.Id, Date = new DateTime(2026, 3, 1), SplitRatio = 2m });

            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = purchaseDate, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = new DateTime(2026, 6, 1), Type = "NA", Value = 1m });
            // precio post-split ajustado: $75 (equivalente a $150 pre-split)
            context.AssetQuotes.Add(new AssetQuote { Asset = stock, Date = new DateTime(2026, 6, 1), Type = "NA", Value = 1m / 75m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetStockStatsAsync(UserId, 0, "BOLSA", true, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].Quantity.Should().Be(20m);        // 10 acciones * factor 2 por el split
            result[0].OriginalValue.Should().Be(1000m); // el costo de compra no cambia por el split
            result[0].ActualValue.Should().Be(1500m);   // 20 * $75 == 10 * $150 (equivalente)
        }

        [Fact]
        public async Task GetStockStatsAsync_WhenConsiderStableIsFalse_ExcludesStableCoins()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var usdt = AddInvestmentAsset(context, "Tether", "USDT", "CRYPTO", "Criptomoneda");

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, usdt, date, amount: 100m, quotePrice: 1m);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = date, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = usdt, Date = date, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);

            (await repo.GetStockStatsAsync(UserId, 0, "CRYPTO", considerStable: false, reference.Id)).Should().BeEmpty();
            (await repo.GetStockStatsAsync(UserId, 0, "CRYPTO", considerStable: true, reference.Id)).Should().ContainSingle();
        }

        [Fact]
        public async Task GetStockStatsAsync_FiltersByEnvironmentAndAssetType()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var stock = AddInvestmentAsset(context, "Apple", "AAPL", "BOLSA", "Accion USA");
            var crypto = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, stock, date, amount: 1m, quotePrice: 1m / 100m);
            AddTransaction(context, crypto, date, amount: 1m, quotePrice: 1m / 100m);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = date, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = stock, Date = date, Type = "NA", Value = 1m / 100m });
            context.AssetQuotes.Add(new AssetQuote { Asset = crypto, Date = date, Type = "NA", Value = 1m / 100m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);

            var bolsaResult = (await repo.GetStockStatsAsync(UserId, 0, "BOLSA", true, reference.Id)).ToList();
            bolsaResult.Should().ContainSingle();
            bolsaResult[0].AssetName.Should().Be("Apple");

            var cryptoResult = (await repo.GetStockStatsAsync(UserId, 0, "CRYPTO", true, reference.Id)).ToList();
            cryptoResult.Should().ContainSingle();
            cryptoResult[0].AssetName.Should().Be("Bitcoin");

            var wrongAssetTypeResult = await repo.GetStockStatsAsync(UserId, stock.AssetTypeId + crypto.AssetTypeId + 999, "BOLSA", true, reference.Id);
            wrongAssetTypeResult.Should().BeEmpty();
        }

        [Fact]
        public async Task GetStockStatsAsync_WhenNetQuantityIsZero_ExcludesAsset()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var stock = AddInvestmentAsset(context, "Apple", "AAPL", "BOLSA");

            var buyDate = new DateTime(2026, 1, 1);
            var sellDate = new DateTime(2026, 2, 1);
            AddTransaction(context, stock, buyDate, amount: 10m, quotePrice: 1m / 100m);
            AddTransaction(context, stock, sellDate, amount: -10m, quotePrice: 1m / 110m);

            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = buyDate, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = stock, Date = sellDate, Type = "NA", Value = 1m / 110m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetStockStatsAsync(UserId, 0, "BOLSA", true, reference.Id);

            result.Should().BeEmpty();
        }

        // ── GetStocksGralStatsAsync (Fase 2) ─────────────────────────────────

        [Fact]
        public async Task GetStocksGralStatsAsync_GroupsByAssetTypeAcrossAssets()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var apple = AddInvestmentAsset(context, "Apple", "AAPL", "BOLSA", "Accion USA");
            var google = AddInvestmentAsset(context, "Google", "GOOGL", "BOLSA", "Accion USA");
            var bond = AddInvestmentAsset(context, "Bono Nacion", "BONA", "BOLSA", "Bono");

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, apple, date, amount: 10m, quotePrice: 1m / 100m);   // $1000
            AddTransaction(context, google, date, amount: 5m, quotePrice: 1m / 200m);   // $1000
            AddTransaction(context, bond, date, amount: 1m, quotePrice: 1m / 500m);     // $500

            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = date, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = apple, Date = date, Type = "NA", Value = 1m / 100m });
            context.AssetQuotes.Add(new AssetQuote { Asset = google, Date = date, Type = "NA", Value = 1m / 200m });
            context.AssetQuotes.Add(new AssetQuote { Asset = bond, Date = date, Type = "NA", Value = 1m / 500m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetStocksGralStatsAsync(UserId, "BOLSA", reference.Id)).ToList();

            result.Should().HaveCount(2); // "Accion USA" (Apple + Google combinadas) y "Bono"
            var accionUsa = result.Single(r => r.AssetType == "Accion USA");
            accionUsa.OriginalValue.Should().Be(2000m); // 1000 + 1000
            accionUsa.ActualValue.Should().Be(2000m);

            var bonoStats = result.Single(r => r.AssetType == "Bono");
            bonoStats.OriginalValue.Should().Be(500m);
        }

        [Fact]
        public async Task GetStocksGralStatsAsync_IncludesStableCoinsUnlikeGetStockStatsAsync()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var usdt = AddInvestmentAsset(context, "Tether", "USDT", "CRYPTO", "Criptomoneda");

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, usdt, date, amount: 100m, quotePrice: 1m);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = date, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = usdt, Date = date, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetStocksGralStatsAsync(UserId, "CRYPTO", reference.Id)).ToList();

            // GetStockGralStats (a diferencia de GetStockStats) nunca tuvo parámetro @ConsiderStable
            result.Should().ContainSingle();
            result[0].AssetType.Should().Be("Criptomoneda");
        }
    }
}
