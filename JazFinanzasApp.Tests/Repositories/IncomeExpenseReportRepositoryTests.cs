using FluentAssertions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.Tests.Repositories
{
    // Fase 12 — T1 (la cuota de tarjeta cuenta como gasto, sin excluir CardTransactionId), T2 (se
    // excluyen los cambios de moneda sin categoría) y equivalencia del total mensual contra el
    // reporte actual (GetIncExpStatsAsync, sin tocar). Mismo patrón de EF InMemory que
    // TransactionRepositoryTests.
    public class IncomeExpenseReportRepositoryTests
    {
        private const int UserId = 1;

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static Asset AddCurrencyAsset(ApplicationDbContext context, string name = "Dolar Estadounidense")
        {
            var assetType = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var asset = new Asset { Name = name, Symbol = "USD", Color = "#000000", AssetType = assetType };
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
            decimal amount, string movementType, decimal quotePrice = 1m, int? cardTransactionId = null, int accountId = 1)
        {
            EnsureAccount(context, accountId);
            var t = new Transaction
            {
                UserId = UserId,
                Asset = asset,
                AccountId = accountId,
                PortfolioId = 1,
                Date = date,
                MovementType = movementType,
                Amount = amount,
                QuotePrice = quotePrice,
                TransactionClass = txClass,
                CardTransactionId = cardTransactionId
            };
            context.Transactions.Add(t);
            return t;
        }

        private static Transaction AddExpense(ApplicationDbContext context, Asset asset, TransactionClass? txClass, DateTime date, decimal amount, int? cardTransactionId = null)
            => AddTransaction(context, asset, txClass, date, -Math.Abs(amount), "E", cardTransactionId: cardTransactionId);

        private static Transaction AddIncome(ApplicationDbContext context, Asset asset, TransactionClass? txClass, DateTime date, decimal amount)
            => AddTransaction(context, asset, txClass, date, Math.Abs(amount), "I");

        // ── T2: los cambios de moneda (TransactionClassId null) se excluyen siempre ────────────

        [Fact]
        public async Task GetIncExpWaterfallAsync_ExcludesCurrencyExchangeTransactions()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var supermercado = AddClass(context, "Supermercado");
            var month = new DateTime(2026, 8, 1);

            AddExpense(context, asset, supermercado, month.AddDays(1), 100m);
            AddExpense(context, asset, null, month.AddDays(2), 250009m); // cambio de moneda, sin clase
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetIncExpWaterfallAsync(UserId, month, asset);

            result.TotalExpense.Should().Be(100m);
        }

        // ── D-3: CountsAsIncomeExpense = false excluye la categoría de los reportes ────────────

        [Fact]
        public async Task GetIncExpWaterfallAsync_ExcludesClassesThatDontCountAsIncomeExpense()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var ajusteSaldos = AddClass(context, "Ajuste Saldos Egreso", countsAsIncomeExpense: false);
            var supermercado = AddClass(context, "Supermercado");
            var month = new DateTime(2026, 8, 1);

            AddExpense(context, asset, ajusteSaldos, month.AddDays(1), 5000m);
            AddExpense(context, asset, supermercado, month.AddDays(2), 100m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetIncExpWaterfallAsync(UserId, month, asset);

            result.TotalExpense.Should().Be(100m);
        }

        // ── T1: la cuota de tarjeta (CardTransactionId != null) cuenta como gasto igual que cualquier otra ──

        [Fact]
        public async Task GetIncExpWaterfallAsync_IncludesTransactionsThatComeFromACardInstallment()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var gastosTarjeta = AddClass(context, "Gastos Tarjeta");
            var month = new DateTime(2026, 8, 1);

            AddExpense(context, asset, gastosTarjeta, month.AddDays(5), 300m, cardTransactionId: 42);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetIncExpWaterfallAsync(UserId, month, asset);

            result.TotalExpense.Should().Be(300m);
            result.ExpenseSteps.Should().ContainSingle(s => s.CategoryName == "Gastos Tarjeta" && s.Amount == 300m);
        }

        // ── Cascada: ingresos, escalón por categoría, resultado y comparación contra el mes anterior ──

        [Fact]
        public async Task GetIncExpWaterfallAsync_BuildsWaterfallWithPreviousMonthResult()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var sueldo = AddClass(context, "Sueldo");
            var super = AddClass(context, "Supermercado");
            var combustible = AddClass(context, "Combustible");
            var month = new DateTime(2026, 8, 1);
            var prevMonth = new DateTime(2026, 7, 1);

            AddIncome(context, asset, sueldo, month.AddDays(1), 2000m);
            AddExpense(context, asset, super, month.AddDays(2), 600m);
            AddExpense(context, asset, combustible, month.AddDays(3), 200m);

            AddIncome(context, asset, sueldo, prevMonth.AddDays(1), 1800m);
            AddExpense(context, asset, super, prevMonth.AddDays(2), 500m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetIncExpWaterfallAsync(UserId, month, asset);

            result.TotalIncome.Should().Be(2000m);
            result.TotalExpense.Should().Be(800m);
            result.Result.Should().Be(1200m);
            result.ExpenseSteps.Should().HaveCount(2);
            result.ExpenseSteps[0].CategoryName.Should().Be("Supermercado"); // mayor a menor
            result.PreviousMonthResult.Should().Be(1300m); // 1800 - 500
        }

        // ── Equivalencia: el total mensual coincide con GetIncExpStatsAsync (sin tocar) ────────

        [Fact]
        public async Task GetIncExpWaterfallAsync_MatchesGetIncExpStatsAsyncMonthlyTotals()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var sueldo = AddClass(context, "Sueldo");
            var super = AddClass(context, "Supermercado");
            var combustible = AddClass(context, "Combustible");
            var ajusteSaldos = AddClass(context, "Ajuste Saldos Egreso", countsAsIncomeExpense: false);
            var month = new DateTime(2026, 8, 1);

            AddIncome(context, asset, sueldo, month.AddDays(1), 2000m);
            AddExpense(context, asset, super, month.AddDays(2), 600m);
            AddExpense(context, asset, combustible, month.AddDays(3), 200m, cardTransactionId: 7);
            AddExpense(context, asset, null, month.AddDays(4), 999m); // cambio de moneda
            AddExpense(context, asset, ajusteSaldos, month.AddDays(5), 5000m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var waterfall = await repo.GetIncExpWaterfallAsync(UserId, month, asset);
            var stats = await repo.GetIncExpStatsAsync(UserId, month, asset);

            waterfall.TotalIncome.Should().Be(stats.ClassIncomeStats.Sum(c => c.Amount));
            waterfall.TotalExpense.Should().Be(stats.ClassExpenseStats.Sum(c => c.Amount));
        }

        [Fact]
        public async Task GetIncExpEvolutionAsync_MonthlyTotalsMatchGetIncExpStatsAsync()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var sueldo = AddClass(context, "Sueldo");
            var super = AddClass(context, "Supermercado");
            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);

            AddIncome(context, asset, sueldo, currentMonthStart.AddDays(1), 1500m);
            AddExpense(context, asset, super, currentMonthStart.AddDays(2), 400m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var evolution = (await repo.GetIncExpEvolutionAsync(UserId, asset, 1)).Single();
            var stats = await repo.GetIncExpStatsAsync(UserId, currentMonthStart, asset);

            evolution.Income.Should().Be(stats.ClassIncomeStats.Sum(c => c.Amount));
            evolution.Expense.Should().Be(stats.ClassExpenseStats.Sum(c => c.Amount));
        }

        // ── D-1/D-2: apertura por rubro — trae el padre de la categoría (T4, un solo hop) ──────

        [Fact]
        public async Task GetSpendingByCategoryMonthlySeriesAsync_ReturnsParentRubroWhenClassified()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var vivienda = AddClass(context, "Vivienda");
            var alquiler = AddClass(context, "Alquiler", parent: vivienda);
            var month = new DateTime(2026, 8, 1);

            AddExpense(context, asset, alquiler, month.AddDays(1), 1000m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetSpendingByCategoryMonthlySeriesAsync(UserId, asset, month, 1)).Single();

            result.CategoryName.Should().Be("Alquiler");
            result.ParentId.Should().Be(vivienda.Id);
            result.ParentName.Should().Be("Vivienda");
            result.MonthlyTrend.Should().Equal(1000m);
        }

        [Fact]
        public async Task GetSpendingByCategoryMonthlySeriesAsync_UnclassifiedCategoryHasNoParent()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var combustible = AddClass(context, "Combustible");
            var month = new DateTime(2026, 8, 1);

            AddExpense(context, asset, combustible, month.AddDays(1), 300m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetSpendingByCategoryMonthlySeriesAsync(UserId, asset, month, 1)).Single();

            result.ParentId.Should().BeNull();
            result.ParentName.Should().BeNull();
        }

        [Fact]
        public async Task GetSpendingByCategoryMonthlySeriesAsync_BuildsAscendingMonthlyTrend()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var super = AddClass(context, "Supermercado");
            var month = new DateTime(2026, 8, 1);

            AddExpense(context, asset, super, month.AddMonths(-2).AddDays(1), 100m);
            AddExpense(context, asset, super, month.AddMonths(-1).AddDays(1), 200m);
            AddExpense(context, asset, super, month.AddDays(1), 300m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetSpendingByCategoryMonthlySeriesAsync(UserId, asset, month, 3)).Single();

            result.MonthlyTrend.Should().Equal(100m, 200m, 300m);
        }

        // ── D-4: por etiqueta — combina movimientos de cuenta y consumos de tarjeta etiquetados ─

        [Fact]
        public async Task GetSpendingByTagAsync_CombinesTaggedTransactionAndTaggedCardPurchase()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var combustible = AddClass(context, "Combustible");
            var service = AddClass(context, "Service");
            // GetSpendingByTagAsync ventanea siempre contra DateTime.Today (no toma un mes explícito
            // como los demás métodos), así que las fechas de prueba tienen que caer en el mes en curso.
            var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var tag = new Tag { Name = "Auto", UserId = UserId };
            context.Tags.Add(tag);

            var taggedTransaction = AddExpense(context, asset, combustible, month.AddDays(1), 150m);

            var card = new Card { Name = "Visa", UserId = UserId };
            context.Cards.Add(card);
            var cardTransaction = new CardTransaction
            {
                UserId = UserId,
                Card = card,
                Asset = asset,
                TransactionClass = service,
                Date = month.AddDays(2),
                Detail = "Service auto",
                TotalAmount = 80m,
                Installments = 1,
                FirstInstallment = month,
                Repeat = "NO"
            };
            context.CardTransactions.Add(cardTransaction);
            await context.SaveChangesAsync();

            context.TransactionTags.Add(new TransactionTag { Transaction = taggedTransaction, Tag = tag });
            context.CardTransactionTags.Add(new CardTransactionTag { CardTransaction = cardTransaction, Tag = tag });

            // Un gasto sin etiquetar no debería sumar al total.
            AddExpense(context, asset, combustible, month.AddDays(3), 999m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = (await repo.GetSpendingByTagAsync(UserId, asset, 1)).Single();

            result.TagName.Should().Be("Auto");
            result.TotalAmount.Should().Be(230m); // 150 + 80
            result.ByCategory.Should().Contain(c => c.CategoryName == "Combustible" && c.Amount == 150m);
            result.ByCategory.Should().Contain(c => c.CategoryName == "Service" && c.Amount == 80m);
        }

        [Fact]
        public async Task GetSpendingByTagAsync_UntaggedExpensesAreNeverIncluded()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var combustible = AddClass(context, "Combustible");
            AddExpense(context, asset, combustible, DateTime.Today, 500m);
            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var result = await repo.GetSpendingByTagAsync(UserId, asset, 1);

            result.Should().BeEmpty();
        }

        // ── Calendario de gastos: un monto por día, mismas guardas T1/T2 ────────────────────────

        [Fact]
        public async Task GetDailySpendingAsync_GroupsByDayAndExcludesCurrencyExchange()
        {
            using var context = CreateContext();
            var asset = AddCurrencyAsset(context);
            var super = AddClass(context, "Supermercado");
            var year = 2026;

            AddExpense(context, asset, super, new DateTime(year, 3, 10), 100m);
            AddExpense(context, asset, super, new DateTime(year, 3, 10), 50m); // mismo día, se suma
            AddExpense(context, asset, super, new DateTime(year, 3, 11), 30m);
            AddExpense(context, asset, null, new DateTime(year, 3, 12), 5000m); // cambio de moneda

            await context.SaveChangesAsync();

            var repo = new TransactionRepository(context);
            var days = (await repo.GetDailySpendingAsync(UserId, asset, year)).ToList();

            days.Should().HaveCount(2);
            days.Should().Contain(d => d.Date == new DateTime(year, 3, 10) && d.Amount == 150m);
            days.Should().Contain(d => d.Date == new DateTime(year, 3, 11) && d.Amount == 30m);
        }
    }
}
