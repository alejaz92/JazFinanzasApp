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

        // Account es una FK requerida de Transaction: GetInvestmentValueContributionsAsync (Fase 2 de
        // docs/plans/activos/portfolios-estadisticas.md) proyecta t.Account.Name, que EF traduce como INNER
        // JOIN — sin una fila de Account, la transacción queda excluida en vez de con AccountName null.
        // Se asegura acá, en el único punto de entrada usado por todos los tests, para no tener que
        // modificarlos uno por uno.
        private static void EnsureAccount(ApplicationDbContext context, int accountId)
        {
            var exists = context.Accounts.Local.Any(a => a.Id == accountId) || context.Accounts.Any(a => a.Id == accountId);
            if (!exists)
                context.Accounts.Add(new Account { Id = accountId, Name = $"Cuenta {accountId}", UserId = UserId });
        }

        private static Transaction AddTransaction(ApplicationDbContext context, Asset asset, DateTime date, decimal amount, decimal quotePrice, int portfolioId = 1, int accountId = 1)
        {
            EnsureAccount(context, accountId);
            var transaction = new Transaction
            {
                UserId = UserId,
                Asset = asset,
                AccountId = accountId,
                PortfolioId = portfolioId,
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

        // ── GetCryptoStatsByDateAsync (Fase 3) ───────────────────────────────
        // El SP original empareja la cotización de referencia por fecha EXACTA (no "la más reciente
        // disponible" como GetStockStats), no redondea el valor final, y un día solo aparece en el
        // resultado si el activo tiene una cotización cargada para esa fecha puntual.

        [Fact]
        public async Task GetCryptoStatsByDateAsync_ComputesDailyValueUsingExactDateReferenceQuote()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");

            var day1 = new DateTime(2026, 1, 1);
            AddTransaction(context, btc, day1, amount: 10m, quotePrice: 0m);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = day1, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = day1, Type = "NA", Value = 1m / 100m }); // $100

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetCryptoStatsByDateAsync(UserId, btc.AssetTypeId, "CRYPTO", btc.Id, true, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].Date.Should().Be(day1);
            result[0].Value.Should().Be(1000m); // 10 BTC * $100
        }

        [Fact]
        public async Task GetCryptoStatsByDateAsync_WhenAssetHasNoQuoteOnExactDay_ExcludesThatDay()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");

            var day1 = new DateTime(2026, 1, 1);
            var day2 = new DateTime(2026, 1, 2);
            AddTransaction(context, btc, day1, amount: 10m, quotePrice: 0m);
            AddTransaction(context, btc, day2, amount: 5m, quotePrice: 0m);

            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = day1, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = day2, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = day1, Type = "NA", Value = 1m / 100m });
            // sin cotización de btc para day2 (gap de carga de precios)

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetCryptoStatsByDateAsync(UserId, btc.AssetTypeId, "CRYPTO", btc.Id, true, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].Date.Should().Be(day1);
        }

        [Fact]
        public async Task GetCryptoStatsByDateAsync_WhenReferenceQuoteMissingForExactDay_DefaultsToOne()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");

            var day1 = new DateTime(2026, 1, 1);
            AddTransaction(context, btc, day1, amount: 10m, quotePrice: 0m);
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = day1, Type = "NA", Value = 1m / 100m });
            // sin ninguna cotización del activo de referencia

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetCryptoStatsByDateAsync(UserId, btc.AssetTypeId, "CRYPTO", btc.Id, true, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].Value.Should().Be(1000m); // referencia por defecto = 1
        }

        [Fact]
        public async Task GetCryptoStatsByDateAsync_DoesNotRoundFinalValue()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");

            var day1 = new DateTime(2026, 1, 1);
            AddTransaction(context, btc, day1, amount: 1m, quotePrice: 0m);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = day1, Type = "NA", Value = 1.001m });
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = day1, Type = "NA", Value = 1m / 3m }); // precio $3

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetCryptoStatsByDateAsync(UserId, btc.AssetTypeId, "CRYPTO", btc.Id, true, reference.Id)).ToList();

            result[0].Value.Should().BeApproximately(3.003m, 0.0000001m); // 1 / (1/3) * 1.001 -- sin redondear a 2 decimales
        }

        [Fact]
        public async Task GetCryptoStatsByDateAsync_WhenAssetIdIsZero_AggregatesAcrossAssetsOfSameType()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);

            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var btc = new Asset { Name = "Bitcoin", Symbol = "BTC", Color = "#000000", AssetType = cryptoType };
            var eth = new Asset { Name = "Ethereum", Symbol = "ETH", Color = "#000000", AssetType = cryptoType };
            context.Assets.AddRange(btc, eth);

            var day1 = new DateTime(2026, 1, 1);
            AddTransaction(context, btc, day1, amount: 10m, quotePrice: 0m); // 10 * $100 = 1000
            AddTransaction(context, eth, day1, amount: 2m, quotePrice: 0m);  // 2 * $50 = 100

            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = day1, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = day1, Type = "NA", Value = 1m / 100m });
            context.AssetQuotes.Add(new AssetQuote { Asset = eth, Date = day1, Type = "NA", Value = 1m / 50m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);

            var aggregated = (await repo.GetCryptoStatsByDateAsync(UserId, cryptoType.Id, "CRYPTO", 0, true, reference.Id)).ToList();
            aggregated.Should().ContainSingle();
            aggregated[0].Value.Should().Be(1100m); // 1000 + 100

            var onlyBtc = (await repo.GetCryptoStatsByDateAsync(UserId, cryptoType.Id, "CRYPTO", btc.Id, true, reference.Id)).ToList();
            onlyBtc.Should().ContainSingle();
            onlyBtc[0].Value.Should().Be(1000m);
        }

        // ── GetInvestmentsHoldingsStats (Fase 4) ─────────────────────────────
        // Solo considera Transaction que forman parte de un InvestmentTransaction; la cotización de
        // referencia usa "la más reciente <= fecha" pero con semántica de INNER JOIN (si no hay ninguna,
        // la transacción se excluye, a diferencia del fallback a 1 de GetCryptoStatsByDate); un CommerceType
        // "Trading" con stablecoin se fuerza a 0 aunque se incluyan stables; se rellenan con 0 todos los
        // meses del calendario para cada CommerceType visto en el rango.

        private static InvestmentTransaction AddInvestmentTransaction(ApplicationDbContext context, string commerceType, Transaction transaction)
        {
            var investmentTransaction = new InvestmentTransaction
            {
                Date = transaction.Date,
                Environment = "CRYPTO",
                MovementType = "I",
                CommerceType = commerceType,
                UserId = UserId,
                IncomeTransaction = transaction
            };
            context.InvestmentTransactions.Add(investmentTransaction);
            return investmentTransaction;
        }

        [Fact]
        public async Task GetInvestmentsHoldingsStats_BasicCase_ComputesValuePerMonthAndCommerceType()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var btc = new Asset { Name = "Bitcoin", Symbol = "BTC", Color = "#000000", AssetType = cryptoType };
            context.Assets.Add(btc);

            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var txnDate = thisMonthStart.AddDays(5);

            var txn = AddTransaction(context, btc, txnDate, amount: 10m, quotePrice: 1m / 100m);
            AddInvestmentTransaction(context, "Deposit", txn);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = thisMonthStart, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetInvestmentsHoldingsStats(UserId, cryptoType.Id, "CRYPTO", 0, true, 1, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].Date.Should().Be(thisMonthStart);
            result[0].CommerceType.Should().Be("Deposit");
            result[0].Value.Should().Be(1000m); // 10 * $100
        }

        [Fact]
        public async Task GetInvestmentsHoldingsStats_TradingWithStableCoin_ZeroesValueButKeepsRow()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var usdt = new Asset { Name = "Tether", Symbol = "USDT", Color = "#000000", AssetType = cryptoType };
            context.Assets.Add(usdt);

            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var txnDate = thisMonthStart.AddDays(3);

            var txn = AddTransaction(context, usdt, txnDate, amount: 100m, quotePrice: 1m);
            AddInvestmentTransaction(context, "Trading", txn);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = thisMonthStart, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetInvestmentsHoldingsStats(UserId, cryptoType.Id, "CRYPTO", 0, considerStable: true, 1, reference.Id)).ToList();

            result.Should().ContainSingle();
            result[0].CommerceType.Should().Be("Trading");
            result[0].Value.Should().Be(0m); // stablecoin + Trading -> forzado a 0 aunque se incluyan stables
        }

        [Fact]
        public async Task GetInvestmentsHoldingsStats_WhenConsiderStableFalse_ExcludesStableCoinRowEntirely()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var usdt = new Asset { Name = "Tether", Symbol = "USDT", Color = "#000000", AssetType = cryptoType };
            context.Assets.Add(usdt);

            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var txnDate = thisMonthStart.AddDays(3);

            var txn = AddTransaction(context, usdt, txnDate, amount: 100m, quotePrice: 1m);
            AddInvestmentTransaction(context, "Trading", txn);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = thisMonthStart, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetInvestmentsHoldingsStats(UserId, cryptoType.Id, "CRYPTO", 0, considerStable: false, 1, reference.Id);

            result.Should().BeEmpty(); // excluida por completo por el WHERE, ni siquiera aparece el CommerceType
        }

        [Fact]
        public async Task GetInvestmentsHoldingsStats_WhenNoReferenceQuoteAvailable_ExcludesTransaction()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var btc = new Asset { Name = "Bitcoin", Symbol = "BTC", Color = "#000000", AssetType = cryptoType };
            context.Assets.Add(btc);

            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var txn = AddTransaction(context, btc, thisMonthStart.AddDays(5), amount: 10m, quotePrice: 1m / 100m);
            AddInvestmentTransaction(context, "Deposit", txn);
            // sin ninguna cotización del activo de referencia

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetInvestmentsHoldingsStats(UserId, cryptoType.Id, "CRYPTO", 0, true, 1, reference.Id);

            result.Should().BeEmpty(); // INNER JOIN: sin cotización de referencia, la transacción se excluye (no cae a 1)
        }

        [Fact]
        public async Task GetInvestmentsHoldingsStats_FillsMonthsWithZeroForCommerceTypesSeenElsewhere()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var btc = new Asset { Name = "Bitcoin", Symbol = "BTC", Color = "#000000", AssetType = cryptoType };
            context.Assets.Add(btc);

            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var txnDate = lastMonthStart.AddDays(2);

            var txn = AddTransaction(context, btc, txnDate, amount: 5m, quotePrice: 1m / 100m);
            AddInvestmentTransaction(context, "Deposit", txn);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = lastMonthStart, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetInvestmentsHoldingsStats(UserId, cryptoType.Id, "CRYPTO", 0, true, 2, reference.Id)).ToList();

            result.Should().HaveCount(2); // mes pasado y mes actual, mismo CommerceType
            result.Should().Contain(r => r.Date == lastMonthStart && r.CommerceType == "Deposit" && r.Value == 500m);
            result.Should().Contain(r => r.Date == thisMonthStart && r.CommerceType == "Deposit" && r.Value == 0m);
        }

        [Fact]
        public async Task GetInvestmentsHoldingsStats_WhenMultipleReferenceQuoteTypesShareResolvedDate_SumsEachContributionSeparately()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context, "Peso Argentino"); // referencia con más de un Type el mismo día, como en la realidad
            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var btc = new Asset { Name = "Bitcoin", Symbol = "BTC", Color = "#000000", AssetType = cryptoType };
            context.Assets.Add(btc);

            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var txnDate = thisMonthStart.AddDays(5);

            var txn = AddTransaction(context, btc, txnDate, amount: 10m, quotePrice: 1m / 100m); // 10 * $100 = 1000 en USD
            AddInvestmentTransaction(context, "Deposit", txn);
            // dos Type de referencia el mismo día -> fan-out esperado
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = thisMonthStart, Type = "NA", Value = 1000m });
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = thisMonthStart, Type = "BLUE", Value = 1050m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetInvestmentsHoldingsStats(UserId, cryptoType.Id, "CRYPTO", 0, true, 1, reference.Id)).ToList();

            result.Should().ContainSingle();
            // fan-out: 1000*1000 + 1000*1050, NO 1000*(1000+1050)
            result[0].Value.Should().Be(2_050_000m);
        }

        [Fact]
        public async Task GetInvestmentsHoldingsStats_WhenAssetIdSpecified_OnlyIncludesThatAsset()
        {
            using var context = CreateContext();
            var reference = AddReferenceAsset(context);
            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var btc = new Asset { Name = "Bitcoin", Symbol = "BTC", Color = "#000000", AssetType = cryptoType };
            var eth = new Asset { Name = "Ethereum", Symbol = "ETH", Color = "#000000", AssetType = cryptoType };
            context.Assets.AddRange(btc, eth);

            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var txnDate = thisMonthStart.AddDays(1);

            var btcTxn = AddTransaction(context, btc, txnDate, amount: 10m, quotePrice: 1m / 100m);
            var ethTxn = AddTransaction(context, eth, txnDate, amount: 2m, quotePrice: 1m / 50m);
            AddInvestmentTransaction(context, "Deposit", btcTxn);
            AddInvestmentTransaction(context, "Deposit", ethTxn);
            context.AssetQuotes.Add(new AssetQuote { Asset = reference, Date = thisMonthStart, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);

            var onlyBtcResult = (await repo.GetInvestmentsHoldingsStats(UserId, cryptoType.Id, "CRYPTO", btc.Id, true, 1, reference.Id)).ToList();
            onlyBtcResult.Should().ContainSingle();
            onlyBtcResult[0].Value.Should().Be(1000m); // solo BTC: 10 * $100

            var allResult = (await repo.GetInvestmentsHoldingsStats(UserId, cryptoType.Id, "CRYPTO", 0, true, 1, reference.Id)).ToList();
            allResult.Should().ContainSingle();
            allResult[0].Value.Should().Be(1100m); // BTC ($1000) + ETH ($100)
        }

        // ── GetTotalsBalanceByUserAsync (Fase 5) ─────────────────────────────
        // Unifica las tres ramas casi idénticas (pesos / dólares / "otro activo") que tenía este método,
        // distinguidas por Asset.Name en vez de un parámetro genérico. El activo con Id=2 (asumido Dólar
        // Estadounidense) es el pivote hardcodeado: se suma directo, sin cotización.

        private static Asset AddDollarPivotAsset(ApplicationDbContext context)
        {
            var assetType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var asset = new Asset { Id = 2, Name = "Dolar Estadounidense", Symbol = "USD", Color = "#000000", AssetType = assetType };
            context.AssetTypes.Add(assetType);
            context.Assets.Add(asset);
            return asset;
        }

        [Fact]
        public async Task GetTotalsBalanceByUserAsync_ForDollarPivot_SumsPivotDirectlyAndConvertsOthersViaLatestQuote()
        {
            using var context = CreateContext();
            var dollar = AddDollarPivotAsset(context);
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 100m, quotePrice: 0m);
            AddTransaction(context, btc, date, amount: 1m, quotePrice: 0m);
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = date, Type = "NA", Value = 1m / 50000m }); // 1 BTC = $50.000

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetTotalsBalanceByUserAsync(UserId, dollar);

            result.Balance.Should().Be(50100m); // 100 (pivote directo) + 50000 (BTC convertido)
        }

        [Fact]
        public async Task GetTotalsBalanceByUserAsync_ForPesoArgentino_ConvertsUsingItsOwnLatestBolsaQuote()
        {
            using var context = CreateContext();
            var dollar = AddDollarPivotAsset(context);
            var pesoType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            context.AssetTypes.Add(pesoType);
            var peso = new Asset { Name = "Peso Argentino", Symbol = "ARS", Color = "#000000", AssetType = pesoType };
            context.Assets.Add(peso);

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 100m, quotePrice: 0m);
            context.AssetQuotes.Add(new AssetQuote { Asset = peso, Date = date, Type = "BOLSA", Value = 1000m }); // 1000 ARS/USD

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetTotalsBalanceByUserAsync(UserId, peso);

            result.Balance.Should().Be(100_000m); // 100 USD * 1000 ARS/USD
        }

        [Fact]
        public async Task GetTotalsBalanceByUserAsync_ForOtherAsset_ConvertsUsingItsOwnLatestQuote()
        {
            using var context = CreateContext();
            var dollar = AddDollarPivotAsset(context);
            var euroType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            context.AssetTypes.Add(euroType);
            var euro = new Asset { Name = "Euro", Symbol = "EUR", Color = "#000000", AssetType = euroType };
            context.Assets.Add(euro);

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 100m, quotePrice: 0m);
            context.AssetQuotes.Add(new AssetQuote { Asset = euro, Date = date, Type = "NA", Value = 0.9m }); // 0.9 EUR/USD

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetTotalsBalanceByUserAsync(UserId, euro);

            result.Balance.Should().Be(90m); // 100 USD * 0.9
        }

        [Fact]
        public async Task GetTotalsBalanceByUserAsync_WhenTargetHasNoBolsaQuoteAtAll_ReturnsZero()
        {
            using var context = CreateContext();
            var dollar = AddDollarPivotAsset(context);
            var pesoType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            context.AssetTypes.Add(pesoType);
            var peso = new Asset { Name = "Peso Argentino", Symbol = "ARS", Color = "#000000", AssetType = pesoType };
            context.Assets.Add(peso);

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 100m, quotePrice: 0m);
            // ninguna cotización Type='BOLSA' para el peso -> sin tasa de conversión posible

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetTotalsBalanceByUserAsync(UserId, peso);

            result.Balance.Should().Be(0m);
        }

        [Fact]
        public async Task GetTotalsBalanceByUserAsync_WhenTransactionAssetHasNoLatestQuote_ContributesZero()
        {
            using var context = CreateContext();
            var dollar = AddDollarPivotAsset(context);
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 100m, quotePrice: 0m);
            AddTransaction(context, btc, date, amount: 1m, quotePrice: 0m);
            // sin ninguna cotización para btc

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetTotalsBalanceByUserAsync(UserId, dollar);

            result.Balance.Should().Be(100m); // solo el pivote; BTC aporta 0 sin cotización
        }

        [Fact]
        public async Task GetTotalsBalanceByUserAsync_WhenAssetsHaveDifferentLatestQuoteDates_EachUsesItsOwnLatestQuote()
        {
            // Desviación deliberada respecto al SP original (ver comentario en el método): un activo cotizado
            // con menor frecuencia que otros (ej. un FCI mensual vs. cripto diario) debe seguir aportando su
            // propio último valor conocido, no quedar en $0 solo porque otro activo tiene una cotización más
            // reciente. Se verificó contra datos reales de Azure que esto afectaba fondos con tenencia real.
            using var context = CreateContext();
            var dollar = AddDollarPivotAsset(context);

            // IDs explícitos: con dos activos adicionales además del dólar (Id=2 fijo), el generador de
            // claves del proveedor InMemory puede asignarle Id=2 a alguno de ellos y colisionar si se
            // dejan autogenerar.
            var cryptoType = new AssetType { Name = "Criptomoneda", Environment = "CRYPTO" };
            context.AssetTypes.Add(cryptoType);
            var btc = new Asset { Id = 10, Name = "Bitcoin", Symbol = "BTC", Color = "#000000", AssetType = cryptoType };
            var eth = new Asset { Id = 11, Name = "Ethereum", Symbol = "ETH", Color = "#000000", AssetType = cryptoType };
            context.Assets.AddRange(btc, eth);

            var olderDate = new DateTime(2026, 1, 1);
            var newerDate = new DateTime(2026, 2, 1);

            AddTransaction(context, dollar, olderDate, amount: 100m, quotePrice: 0m);
            AddTransaction(context, btc, olderDate, amount: 1m, quotePrice: 0m);
            AddTransaction(context, eth, olderDate, amount: 1m, quotePrice: 0m);

            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = olderDate, Type = "NA", Value = 1m / 50000m }); // BTC: solo cotización vieja
            context.AssetQuotes.Add(new AssetQuote { Asset = eth, Date = newerDate, Type = "NA", Value = 1m / 2000m }); // ETH: tiene una cotización más nueva

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetTotalsBalanceByUserAsync(UserId, dollar);

            result.Balance.Should().Be(52100m); // 100 (pivote) + 50000 (BTC, su propia última cotización) + 2000 (ETH, la suya)
        }

        [Fact]
        public async Task GetTotalsBalanceByUserAsync_WhenMultipleQuoteTypesShareLatestDate_SumsEachContributionSeparately()
        {
            using var context = CreateContext();
            var dollar = AddDollarPivotAsset(context);
            var pesoType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            context.AssetTypes.Add(pesoType);
            var peso = new Asset { Name = "Peso Argentino", Symbol = "ARS", Color = "#000000", AssetType = pesoType };
            context.Assets.Add(peso);

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, peso, date, amount: 5000m, quotePrice: 0m);
            // dos Type distintos el mismo día para el propio Peso Argentino (NA y BOLSA)
            context.AssetQuotes.Add(new AssetQuote { Asset = peso, Date = date, Type = "NA", Value = 1000m });
            context.AssetQuotes.Add(new AssetQuote { Asset = peso, Date = date, Type = "BOLSA", Value = 1050m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetTotalsBalanceByUserAsync(UserId, dollar);

            // fan-out: 5000/1000 + 5000/1050, NO 5000/(1000+1050)
            var expected = Math.Round(5000m / 1000m + 5000m / 1050m, 2);
            result.Balance.Should().Be(expected);
        }

        [Fact]
        public async Task GetTotalsBalanceByUserAsync_WhenNoTransactions_ReturnsZeroWithoutError()
        {
            using var context = CreateContext();
            var dollar = AddDollarPivotAsset(context);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetTotalsBalanceByUserAsync(UserId, dollar);

            result.Balance.Should().Be(0m);
        }

        // ── GetPortfolioStatsAsync (docs/plans/activos/portfolios-estadisticas.md, Fase 1) ──────────
        // A diferencia de GetStockStatsAsync/GetStocksGralStatsAsync, combina efectivo e inversión en un
        // solo total por cartera (sin filtro de Environment) y siempre incluye las carteras del usuario
        // aunque no tengan ninguna transacción.

        [Fact]
        public async Task GetPortfolioStatsAsync_CombinesCashAndInvestmentIntoSingleTotal()
        {
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            var apple = AddInvestmentAsset(context, "Apple", "AAPL", "BOLSA");
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Corto Plazo", UserId = UserId });

            var purchaseDate = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, purchaseDate, amount: 500m, quotePrice: 1m, portfolioId: 1); // efectivo
            AddTransaction(context, apple, purchaseDate, amount: 10m, quotePrice: 1m / 100m, portfolioId: 1); // 10 acciones @ $100

            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = purchaseDate, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = new DateTime(2026, 6, 1), Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = apple, Date = new DateTime(2026, 6, 1), Type = "NA", Value = 1m / 150m }); // última cotización: $150

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetPortfolioStatsAsync(UserId, dollar.Id)).ToList();

            result.Should().ContainSingle();
            result[0].PortfolioId.Should().Be(1);
            result[0].OriginalValue.Should().Be(1500m); // 500 (efectivo) + 1000 (10 * $100)
            result[0].ActualValue.Should().Be(2000m);    // 500 (efectivo) + 1500 (10 * $150)
        }

        [Fact]
        public async Task GetPortfolioStatsAsync_PortfolioWithoutTransactions_ReturnsZeroWithoutBreaking()
        {
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Viajes", UserId = UserId });
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetPortfolioStatsAsync(UserId, dollar.Id)).ToList();

            result.Should().ContainSingle();
            result[0].PortfolioId.Should().Be(1);
            result[0].OriginalValue.Should().Be(0m);
            result[0].ActualValue.Should().Be(0m);
        }

        [Fact]
        public async Task GetPortfolioStatsAsync_PortfolioWithOnlyCash_ComputesValueWithoutInvestment()
        {
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Viajes", UserId = UserId });

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 850m, quotePrice: 1m, portfolioId: 1);
            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = date, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetPortfolioStatsAsync(UserId, dollar.Id)).ToList();

            result.Should().ContainSingle();
            result[0].OriginalValue.Should().Be(850m);
            result[0].ActualValue.Should().Be(850m);
        }

        [Fact]
        public async Task GetPortfolioStatsAsync_PortfolioExchange_MovesValueBetweenPortfolios()
        {
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Corto Plazo", UserId = UserId });
            context.Portfolios.Add(new Portfolio { Id = 2, Name = "Jubilacion", UserId = UserId });

            var fundingDate = new DateTime(2026, 1, 1);
            var exchangeDate = new DateTime(2026, 2, 1);
            AddTransaction(context, dollar, fundingDate, amount: 1000m, quotePrice: 1m, portfolioId: 1);
            // portfolioExchange: par de transacciones que mueven 300 de la cartera 1 a la cartera 2
            AddTransaction(context, dollar, exchangeDate, amount: -300m, quotePrice: 1m, portfolioId: 1);
            AddTransaction(context, dollar, exchangeDate, amount: 300m, quotePrice: 1m, portfolioId: 2);

            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = exchangeDate, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetPortfolioStatsAsync(UserId, dollar.Id)).ToList();

            result.Should().HaveCount(2);
            result.Single(r => r.PortfolioId == 1).ActualValue.Should().Be(700m);
            result.Single(r => r.PortfolioId == 2).ActualValue.Should().Be(300m);
        }

        [Fact]
        public async Task GetPortfolioStatsAsync_AssetsWithDifferentQuoteFrequencies_EachUsesItsOwnLatestQuote()
        {
            // Una cartera puede mezclar cripto (cotización diaria) con un FCI (cotización mensual) sin
            // ningún problema conceptual: cada activo debe valorizarse con su propia última cotización,
            // no quedar en $0 porque otro activo de la misma cartera tiene una cotización más reciente.
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");
            var fci = AddInvestmentAsset(context, "Fondo Renta Fija", "FRF", "BOLSA", "FCI");
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Jubilacion", UserId = UserId });

            var purchaseDate = new DateTime(2026, 1, 1);
            var cryptoQuoteDate = new DateTime(2026, 6, 15); // cotización diaria, más reciente
            var fciQuoteDate = new DateTime(2026, 6, 1);     // cotización mensual, más vieja que la de btc

            AddTransaction(context, btc, purchaseDate, amount: 1m, quotePrice: 1m / 50000m, portfolioId: 1);
            AddTransaction(context, fci, purchaseDate, amount: 100m, quotePrice: 1m / 10m, portfolioId: 1);

            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = purchaseDate, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = cryptoQuoteDate, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = cryptoQuoteDate, Type = "NA", Value = 1m / 60000m });
            context.AssetQuotes.Add(new AssetQuote { Asset = fci, Date = fciQuoteDate, Type = "NA", Value = 1m / 12m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetPortfolioStatsAsync(UserId, dollar.Id)).ToList();

            result.Should().ContainSingle();
            // btc: 1 * $60000 = 60000; fci: 100 * $12 = 1200 -- ambos cuentan pese a cotizar con frecuencia distinta
            result[0].ActualValue.Should().Be(61200m);
        }

        // ── GetPortfolioHoldingsAsync (docs/plans/activos/portfolios-estadisticas.md, Fase 2) ────────

        [Fact]
        public async Task GetPortfolioHoldingsAsync_GroupsByAssetTypeIncludingCash()
        {
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            var apple = AddInvestmentAsset(context, "Apple", "AAPL", "BOLSA", "Accion USA");
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Jubilacion", UserId = UserId });

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 500m, quotePrice: 1m, portfolioId: 1);
            AddTransaction(context, apple, date, amount: 10m, quotePrice: 1m / 100m, portfolioId: 1);
            AddTransaction(context, btc, date, amount: 1m, quotePrice: 1m / 50000m, portfolioId: 1);

            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = date, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = apple, Date = date, Type = "NA", Value = 1m / 100m });
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = date, Type = "NA", Value = 1m / 50000m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetPortfolioHoldingsAsync(UserId, 1, dollar.Id)).ToList();

            result.Should().HaveCount(3);
            result.Select(r => r.AssetType).Should().BeEquivalentTo(new[] { "Moneda", "Accion USA", "Criptomoneda" });
            result.Single(r => r.AssetType == "Moneda").ActualValue.Should().Be(500m);
            result.Single(r => r.AssetType == "Accion USA").ActualValue.Should().Be(1000m);
            result.Single(r => r.AssetType == "Criptomoneda").ActualValue.Should().Be(50000m);
        }

        [Fact]
        public async Task GetPortfolioHoldingsAsync_AssetSplitAcrossAccounts_ReturnsOneRowPerAccount()
        {
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Jubilacion", UserId = UserId });

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, btc, date, amount: 0.02m, quotePrice: 1m / 50000m, portfolioId: 1, accountId: 1); // Binance
            AddTransaction(context, btc, date, amount: 0.03m, quotePrice: 1m / 50000m, portfolioId: 1, accountId: 2); // Lemon

            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = date, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = date, Type = "NA", Value = 1m / 50000m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetPortfolioHoldingsAsync(UserId, 1, dollar.Id)).ToList();

            result.Should().HaveCount(2); // una fila por cuenta, no una sola fila agregada
            result.Select(r => r.AccountName).Should().BeEquivalentTo(new[] { "Cuenta 1", "Cuenta 2" });
            result.Single(r => r.AccountName == "Cuenta 1").Quantity.Should().Be(0.02m);
            result.Single(r => r.AccountName == "Cuenta 2").Quantity.Should().Be(0.03m);
            result.Sum(r => r.Quantity).Should().Be(0.05m);
            result.Sum(r => r.ActualValue).Should().Be(2500m); // 0.05 BTC * $50000, repartido entre las dos filas
        }

        [Fact]
        public async Task GetPortfolioHoldingsAsync_SumOfActualValues_MatchesGetPortfolioStatsAsyncTotal()
        {
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            var apple = AddInvestmentAsset(context, "Apple", "AAPL", "BOLSA", "Accion USA");
            var btc = AddInvestmentAsset(context, "Bitcoin", "BTC", "CRYPTO", "Criptomoneda");
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Jubilacion", UserId = UserId });

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 500m, quotePrice: 1m, portfolioId: 1, accountId: 1);
            AddTransaction(context, apple, date, amount: 10m, quotePrice: 1m / 100m, portfolioId: 1, accountId: 1);
            AddTransaction(context, btc, date, amount: 0.02m, quotePrice: 1m / 50000m, portfolioId: 1, accountId: 1);
            AddTransaction(context, btc, date, amount: 0.03m, quotePrice: 1m / 50000m, portfolioId: 1, accountId: 2);

            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = date, Type = "NA", Value = 1m });
            context.AssetQuotes.Add(new AssetQuote { Asset = apple, Date = date, Type = "NA", Value = 1m / 100m });
            context.AssetQuotes.Add(new AssetQuote { Asset = btc, Date = date, Type = "NA", Value = 1m / 50000m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var stats = (await repo.GetPortfolioStatsAsync(UserId, dollar.Id)).Single(s => s.PortfolioId == 1);
            var holdings = (await repo.GetPortfolioHoldingsAsync(UserId, 1, dollar.Id)).ToList();

            holdings.Sum(h => h.ActualValue).Should().Be(stats.ActualValue);
        }

        [Fact]
        public async Task GetPortfolioHoldingsAsync_OnlyReturnsHoldingsForRequestedPortfolio()
        {
            using var context = CreateContext();
            var dollar = AddReferenceAsset(context);
            context.Portfolios.Add(new Portfolio { Id = 1, Name = "Corto Plazo", UserId = UserId });
            context.Portfolios.Add(new Portfolio { Id = 2, Name = "Jubilacion", UserId = UserId });

            var date = new DateTime(2026, 1, 1);
            AddTransaction(context, dollar, date, amount: 500m, quotePrice: 1m, portfolioId: 1);
            AddTransaction(context, dollar, date, amount: 900m, quotePrice: 1m, portfolioId: 2);
            context.AssetQuotes.Add(new AssetQuote { Asset = dollar, Date = date, Type = "NA", Value = 1m });

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetPortfolioHoldingsAsync(UserId, 1, dollar.Id)).ToList();

            result.Should().ContainSingle();
            result[0].ActualValue.Should().Be(500m);
        }
    }
}
