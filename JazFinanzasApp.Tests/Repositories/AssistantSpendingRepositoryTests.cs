using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.Tests.Repositories
{
    // plan-devoluciones-llm.md, Fase 2 — las dos consultas que el asistente agrega a
    // TransactionRepository: el gasto por categoría "a la misma altura del mes" y los egresos
    // individuales más grandes. Mismo patrón de EF InMemory que IncomeExpenseReportRepositoryTests.
    public class AssistantSpendingRepositoryTests
    {
        private const int UserId = 1;

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static Asset AddCurrencyAsset(ApplicationDbContext context, string name, string symbol)
        {
            var assetType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var asset = new Asset { Name = name, Symbol = symbol, Color = "#000000", AssetType = assetType };
            context.AssetTypes.Add(assetType);
            context.Assets.Add(asset);
            return asset;
        }

        private static void EnsureAccount(ApplicationDbContext context, int accountId)
        {
            var exists = context.Accounts.Local.Any(a => a.Id == accountId) || context.Accounts.Any(a => a.Id == accountId);
            if (!exists)
                context.Accounts.Add(new Account { Id = accountId, Name = $"Cuenta {accountId}", UserId = UserId });
        }

        private static TransactionClass AddClass(ApplicationDbContext context, string description, bool countsAsIncomeExpense = true, TransactionClass? parent = null)
        {
            var tc = new TransactionClass { Description = description, IncExp = "E", CountsAsIncomeExpense = countsAsIncomeExpense, Parent = parent, UserId = UserId };
            context.TransactionClasses.Add(tc);
            return tc;
        }

        private static Transaction AddTransaction(ApplicationDbContext context, Asset asset, TransactionClass? txClass, DateTime date,
            decimal amount, string movementType, decimal quotePrice = 1m, string? detail = null)
        {
            EnsureAccount(context, 1);
            var t = new Transaction
            {
                UserId = UserId,
                Asset = asset,
                AccountId = 1,
                PortfolioId = 1,
                Date = date,
                MovementType = movementType,
                Amount = amount,
                QuotePrice = quotePrice,
                TransactionClass = txClass,
                Detail = detail
            };
            context.Transactions.Add(t);
            return t;
        }

        private static Transaction AddExpense(ApplicationDbContext context, Asset asset, TransactionClass? txClass, DateTime date, decimal amount, decimal quotePrice = 1m, string? detail = null)
            => AddTransaction(context, asset, txClass, date, -Math.Abs(amount), "E", quotePrice, detail);

        // ── GetSpendingToDateByCategoryAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_CutsEveryMonthAtTheSameDay()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var super = AddClass(context, "Supermercado");
            var month = new DateTime(2026, 9, 1);

            AddExpense(context, ars, super, new DateTime(2026, 9, 10), 100m);   // del tramo
            AddExpense(context, ars, super, new DateTime(2026, 9, 20), 999m);   // después del corte
            AddExpense(context, ars, super, new DateTime(2026, 8, 12), 60m);    // mes anterior, del tramo
            AddExpense(context, ars, super, new DateTime(2026, 8, 25), 500m);   // mes anterior, después del corte
            AddExpense(context, ars, super, new DateTime(2026, 7, 5), 30m);
            AddExpense(context, ars, super, new DateTime(2026, 6, 15), 90m);    // el día de corte entra
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetSpendingToDateByCategoryAsync(UserId, ars, month, 15, 6)).ToList();

            var row = result.Should().ContainSingle().Subject;
            row.CategoryName.Should().Be("Supermercado");
            row.CurrentToDate.Should().Be(100m);
            row.PreviousToDate.Should().Be(60m);
            row.AverageToDate.Should().Be(30m); // (60 + 30 + 90 + 0 + 0 + 0) / 6
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_ClipsTheStretchToTheLengthOfAShorterMonth()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var super = AddClass(context, "Supermercado");

            // Marzo completo (día 31) contra febrero, que solo tiene 28 días: entra entero.
            AddExpense(context, ars, super, new DateTime(2026, 3, 31), 50m);
            AddExpense(context, ars, super, new DateTime(2026, 2, 28), 70m);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetSpendingToDateByCategoryAsync(UserId, ars, new DateTime(2026, 3, 1), 31, 6)).ToList();

            var row = result.Should().ContainSingle().Subject;
            row.CurrentToDate.Should().Be(50m);
            row.PreviousToDate.Should().Be(70m);
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_ExcludesWhatTheOtherIncomeExpenseReportsExclude()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var super = AddClass(context, "Supermercado");
            var ajuste = AddClass(context, "Ajuste Saldos Egreso", countsAsIncomeExpense: false);
            var month = new DateTime(2026, 9, 1);

            AddExpense(context, ars, super, new DateTime(2026, 9, 5), 100m);
            AddExpense(context, ars, ajuste, new DateTime(2026, 9, 5), 5000m);                        // CountsAsIncomeExpense = false
            AddExpense(context, ars, null, new DateTime(2026, 9, 6), 250000m);                        // cambio de moneda, sin clase
            AddTransaction(context, ars, super, new DateTime(2026, 9, 7), 800m, "I");                 // ingreso
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetSpendingToDateByCategoryAsync(UserId, ars, month, 30, 6)).ToList();

            result.Should().ContainSingle().Which.CurrentToDate.Should().Be(100m);
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_KeepsACategoryThatOnlySpentInEarlierMonths()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var super = AddClass(context, "Supermercado");
            var gimnasio = AddClass(context, "Gimnasio");
            var month = new DateTime(2026, 9, 1);

            AddExpense(context, ars, super, new DateTime(2026, 9, 3), 100m);
            AddExpense(context, ars, gimnasio, new DateTime(2026, 5, 3), 120m); // este mes no gastó nada

            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetSpendingToDateByCategoryAsync(UserId, ars, month, 30, 6)).ToList();

            result.Should().HaveCount(2);
            var gym = result.Single(r => r.CategoryName == "Gimnasio");
            gym.CurrentToDate.Should().Be(0m);
            gym.PreviousToDate.Should().Be(0m);
            gym.AverageToDate.Should().Be(20m); // 120 / 6
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_IgnoresMonthsOlderThanTheComparisonWindow()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var super = AddClass(context, "Supermercado");

            AddExpense(context, ars, super, new DateTime(2026, 9, 3), 100m);
            AddExpense(context, ars, super, new DateTime(2026, 2, 3), 9999m); // 7 meses antes: fuera de la ventana de 6
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetSpendingToDateByCategoryAsync(UserId, ars, new DateTime(2026, 9, 1), 30, 6)).ToList();

            result.Should().ContainSingle().Which.AverageToDate.Should().Be(0m);
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_TotalMatchesTheDashboardThermometer()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var super = AddClass(context, "Supermercado");
            var delivery = AddClass(context, "Delivery");

            AddExpense(context, ars, super, new DateTime(2026, 9, 2), 100m);
            AddExpense(context, ars, delivery, new DateTime(2026, 9, 2), 40.5m);
            AddExpense(context, ars, delivery, new DateTime(2026, 9, 14), 60m);
            AddExpense(context, ars, super, new DateTime(2026, 9, 28), 700m);   // después del corte
            AddExpense(context, ars, super, new DateTime(2026, 8, 9), 80m);
            AddExpense(context, ars, delivery, new DateTime(2026, 8, 15), 25m);
            AddExpense(context, ars, delivery, new DateTime(2026, 8, 20), 300m); // después del corte
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var cutoffDay = 15;

            var categories = (await repo.GetSpendingToDateByCategoryAsync(UserId, ars, new DateTime(2026, 9, 1), cutoffDay, 6)).ToList();

            // El termómetro de Inicio: el mismo cálculo a partir del gasto diario del año.
            var days = (await repo.GetDailySpendingAsync(UserId, ars, 2026))
                .Select(d => new DaySpendingDTO { Date = d.Date, Amount = d.Amount })
                .ToList();
            var thermometer = DashboardService.BuildThermometer(days, new DateTime(2026, 9, cutoffDay));

            categories.Sum(c => c.CurrentToDate).Should().Be(thermometer.MonthToDateAmount);
            categories.Sum(c => c.PreviousToDate).Should().Be(thermometer.PreviousMonthSameDayAmount);
            thermometer.MonthToDateAmount.Should().Be(200.5m);
            thermometer.PreviousMonthSameDayAmount.Should().Be(105m);
        }

        [Fact]
        public async Task GetSpendingToDateByCategoryAsync_ReturnsParentRubro()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var comida = AddClass(context, "Comida");
            var super = AddClass(context, "Supermercado", parent: comida);

            AddExpense(context, ars, super, new DateTime(2026, 9, 3), 100m);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context).GetSpendingToDateByCategoryAsync(UserId, ars, new DateTime(2026, 9, 1), 30, 6)).ToList();

            result.Should().ContainSingle().Which.ParentName.Should().Be("Comida");
        }

        // ── GetLargestExpensesAsync ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetLargestExpensesAsync_RanksByTheFirstCurrencyAndReportsTheAmountInBoth()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var usd = AddCurrencyAsset(context, "Dolar Estadounidense", "USD");
            var service = AddClass(context, "Mantenimiento auto");
            var super = AddClass(context, "Supermercado");

            // QuotePrice 1000: 1 dólar = 1000 pesos. Sin cotizaciones cargadas el destino vale 1.
            AddExpense(context, ars, service, new DateTime(2026, 9, 10), 240000m, quotePrice: 1000m, detail: "service");
            AddExpense(context, ars, super, new DateTime(2026, 9, 12), 90000m, quotePrice: 1000m);
            AddExpense(context, ars, super, new DateTime(2026, 9, 13), 15000m, quotePrice: 1000m);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context)
                .GetLargestExpensesAsync(UserId, new[] { ars, usd }, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), 2)).ToList();

            result.Should().HaveCount(2);
            result[0].CategoryName.Should().Be("Mantenimiento auto");
            result[0].Detail.Should().Be("service");
            result[0].AmountByAssetId[ars.Id].Should().Be(240000m);
            result[0].AmountByAssetId[usd.Id].Should().Be(240m);
            result[1].AmountByAssetId[ars.Id].Should().Be(90000m);
        }

        [Fact]
        public async Task GetLargestExpensesAsync_FiltersByCategoryAndByTheHalfOpenDateRange()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var super = AddClass(context, "Supermercado");
            var delivery = AddClass(context, "Delivery");

            AddExpense(context, ars, super, new DateTime(2026, 9, 1), 10m);
            AddExpense(context, ars, super, new DateTime(2026, 9, 20), 50m);
            AddExpense(context, ars, super, new DateTime(2026, 10, 1), 7777m); // toExclusive: no entra
            AddExpense(context, ars, super, new DateTime(2026, 8, 31), 8888m); // antes del rango
            AddExpense(context, ars, delivery, new DateTime(2026, 9, 5), 999m);
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context)
                .GetLargestExpensesAsync(UserId, new[] { ars }, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), 5, super.Id)).ToList();

            result.Select(r => r.AmountByAssetId[ars.Id]).Should().Equal(50m, 10m);
        }

        [Fact]
        public async Task GetLargestExpensesAsync_ExcludesWhatTheOtherIncomeExpenseReportsExclude()
        {
            using var context = CreateContext();
            var ars = AddCurrencyAsset(context, "Peso Argentino", "ARS");
            var super = AddClass(context, "Supermercado");
            var ajuste = AddClass(context, "Ajuste Saldos Egreso", countsAsIncomeExpense: false);

            AddExpense(context, ars, super, new DateTime(2026, 9, 5), 100m);
            AddExpense(context, ars, ajuste, new DateTime(2026, 9, 5), 5000m);
            AddExpense(context, ars, null, new DateTime(2026, 9, 6), 250000m);
            AddTransaction(context, ars, super, new DateTime(2026, 9, 7), 800m, "I");
            await context.SaveChangesAsync();

            var result = (await new TransactionRepository(context)
                .GetLargestExpensesAsync(UserId, new[] { ars }, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), 10)).ToList();

            result.Should().ContainSingle().Which.AmountByAssetId[ars.Id].Should().Be(100m);
        }

        [Fact]
        public async Task GetLargestExpensesAsync_WithoutCurrencies_Throws()
        {
            using var context = CreateContext();

            var act = () => new TransactionRepository(context)
                .GetLargestExpensesAsync(UserId, Array.Empty<Asset>(), new DateTime(2026, 9, 1), new DateTime(2026, 10, 1), 5);

            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
