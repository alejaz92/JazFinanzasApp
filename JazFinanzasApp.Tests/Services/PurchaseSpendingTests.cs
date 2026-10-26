using FluentAssertions;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm-v2.md, Fase 1 — el gasto por fecha de compra (D1). Los tests del calculador son puros; los del repositorio y
    // el servicio usan EF InMemory, como los de las fuentes del asistente.
    public class PurchaseSpendingCalculatorTests
    {
        private const int Ars = 1;
        private const int Usd = 2;

        private static PurchaseSpendingRowResult Row(string category, DateTime date, decimal ars, decimal usd = 0m,
            string kind = PurchaseSpendingKind.Direct, int id = 1, int categoryId = 0, string? parent = null) => new()
        {
            Date = date,
            Kind = kind,
            SourceId = id,
            CategoryId = categoryId == 0 ? category.GetHashCode() & 0xFFFF : categoryId,
            CategoryName = category,
            ParentName = parent,
            AmountByAssetId = new Dictionary<int, decimal> { [Ars] = ars, [Usd] = usd }
        };

        // ── ToDateByCategory ──────────────────────────────────────────────────────────────────

        [Fact]
        public void ToDateByCategory_CutsEveryMonthAtTheSameDayAndAveragesThePreviousOnes()
        {
            var rows = new[]
            {
                Row("Delivery", new DateTime(2026, 9, 10), 100m),   // del tramo
                Row("Delivery", new DateTime(2026, 9, 20), 999m),   // después del corte
                Row("Delivery", new DateTime(2026, 8, 12), 60m),    // mes anterior, del tramo
                Row("Delivery", new DateTime(2026, 8, 25), 500m),   // mes anterior, después del corte
                Row("Delivery", new DateTime(2026, 7, 5), 30m),
                Row("Delivery", new DateTime(2026, 6, 15), 90m)     // el día de corte entra
            };

            var result = PurchaseSpendingCalculator.ToDateByCategory(rows, Ars, new DateTime(2026, 9, 1), 15, 6);

            var row = result.Categories.Should().ContainSingle().Subject;
            row.CurrentToDate.Should().Be(100m);
            row.PreviousToDate.Should().Be(60m);
            row.AverageToDate.Should().Be(30m); // (60 + 30 + 90 + 0 + 0 + 0) / 6, los meses sin gasto cuentan como cero
        }

        [Fact]
        public void ToDateByCategory_ClipsTheStretchToTheLengthOfAShorterMonth()
        {
            // Marzo completo (31) contra febrero, que solo tiene 28 días: entra entero.
            var rows = new[] { Row("Super", new DateTime(2026, 3, 31), 50m), Row("Super", new DateTime(2026, 2, 28), 70m) };

            var result = PurchaseSpendingCalculator.ToDateByCategory(rows, Ars, new DateTime(2026, 3, 1), 31, 1);

            var row = result.Categories.Should().ContainSingle().Subject;
            row.CurrentToDate.Should().Be(50m);
            row.PreviousToDate.Should().Be(70m);
        }

        [Fact]
        public void ToDateByCategory_TotalsAreTheSumOfTheCategoriesAndTheyAreOrderedByWeight()
        {
            var rows = new[]
            {
                Row("Chica", new DateTime(2026, 9, 2), 10.004m),
                Row("Grande", new DateTime(2026, 9, 2), 500m),
                Row("Grande", new DateTime(2026, 9, 3), 20.006m)
            };

            var result = PurchaseSpendingCalculator.ToDateByCategory(rows, Ars, new DateTime(2026, 9, 1), 30, 6);

            result.Categories.Select(c => c.CategoryName).Should().Equal("Grande", "Chica");
            result.TotalCurrent.Should().Be(result.Categories.Sum(c => c.CurrentToDate));
            result.TotalCurrent.Should().Be(530.01m); // 10,00 + 520,01
        }

        [Fact]
        public void ToDateByCategory_DropsACategoryWithoutAnyAmountInTheThreeColumns()
        {
            var rows = new[] { Row("Vieja", new DateTime(2025, 1, 5), 100m) }; // fuera de los 6 meses

            PurchaseSpendingCalculator.ToDateByCategory(rows, Ars, new DateTime(2026, 9, 1), 30, 6).Categories.Should().BeEmpty();
        }

        [Fact]
        public void ToDateByCategory_UsesTheAmountOfTheRequestedCurrency()
        {
            var rows = new[] { Row("Super", new DateTime(2026, 9, 2), 150000m, 100m) };

            PurchaseSpendingCalculator.ToDateByCategory(rows, Usd, new DateTime(2026, 9, 1), 30, 6).TotalCurrent.Should().Be(100m);
            PurchaseSpendingCalculator.ToDateByCategory(rows, Ars, new DateTime(2026, 9, 1), 30, 6).TotalCurrent.Should().Be(150000m);
        }

        // ── DailySeries y Largest ─────────────────────────────────────────────────────────────

        [Fact]
        public void DailySeries_AddsUpEachDayAndOrdersThem()
        {
            var rows = new[]
            {
                Row("A", new DateTime(2026, 9, 3, 18, 0, 0), 10m),
                Row("B", new DateTime(2026, 9, 3, 9, 0, 0), 5m),
                Row("A", new DateTime(2026, 9, 1), 100m)
            };

            var days = PurchaseSpendingCalculator.DailySeries(rows, Ars);

            days.Select(d => (d.Date, d.Amount)).Should().Equal((new DateTime(2026, 9, 1), 100m), (new DateTime(2026, 9, 3), 15m));
        }

        [Fact]
        public void Largest_LeavesTheRecurringOutRanksByTheFirstCurrencyAndBreaksTiesByKindAndId()
        {
            var rows = new[]
            {
                Row("A", new DateTime(2026, 9, 1), 900m, kind: PurchaseSpendingKind.CardRecurring, id: 7),
                Row("B", new DateTime(2026, 9, 2), 300m, kind: PurchaseSpendingKind.Direct, id: 3),
                Row("C", new DateTime(2026, 9, 3), 300m, kind: PurchaseSpendingKind.CardPurchase, id: 2),
                Row("D", new DateTime(2026, 9, 4), 500m, kind: PurchaseSpendingKind.CardPurchase, id: 9)
            };

            var largest = PurchaseSpendingCalculator.Largest(rows, Ars, 3, null);

            largest.Select(r => r.CategoryName).Should().Equal("D", "C", "B"); // C (CardPurchase) antes que B (Direct) en el empate
        }

        [Fact]
        public void Largest_FiltersByCategory()
        {
            var rows = new[]
            {
                Row("A", new DateTime(2026, 9, 1), 900m, categoryId: 1),
                Row("B", new DateTime(2026, 9, 2), 300m, categoryId: 2)
            };

            PurchaseSpendingCalculator.Largest(rows, Ars, 5, 2).Should().ContainSingle().Which.CategoryName.Should().Be("B");
        }

        // ── InstallmentsDue ───────────────────────────────────────────────────────────────────

        [Fact]
        public void InstallmentsDue_SeparatesInstallmentsFromRecurringAndGroupsByCategory()
        {
            var due = new[]
            {
                new CardInstallmentDueResult { CardTransactionId = 1, CategoryId = 1, CategoryName = "Ropa", Amount = 22777.48m },
                new CardInstallmentDueResult { CardTransactionId = 2, CategoryId = 1, CategoryName = "Ropa", Amount = 17578.67m },
                new CardInstallmentDueResult { CardTransactionId = 3, CategoryId = 2, CategoryName = "Streaming", Amount = 9000m, IsRecurring = true }
            };

            var dto = PurchaseSpendingCalculator.InstallmentsDue(due, new DateTime(2026, 9, 17));

            dto.Month.Should().Be(new DateTime(2026, 9, 1));
            dto.TotalInstallments.Should().Be(40356.15m);
            dto.TotalRecurring.Should().Be(9000m);
            dto.Total.Should().Be(49356.15m);
            dto.Categories.Select(c => (c.CategoryName, c.Amount)).Should().Equal(("Ropa", 40356.15m), ("Streaming", 9000m));
        }

        [Fact]
        public void InstallmentsDue_WithNothingDueIsEmpty()
        {
            var dto = PurchaseSpendingCalculator.InstallmentsDue(Array.Empty<CardInstallmentDueResult>(), new DateTime(2026, 9, 1));

            dto.Total.Should().Be(0m);
            dto.Categories.Should().BeEmpty();
        }
    }

    public class PurchaseSpendingRepositoryTests
    {
        private const int UserId = 1;

        private static ApplicationDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static Asset AddCurrency(ApplicationDbContext context, string name, string symbol)
        {
            var type = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var asset = new Asset { Name = name, Symbol = symbol, Color = "#000000", AssetType = type };
            context.AssetTypes.Add(type);
            context.Assets.Add(asset);
            return asset;
        }

        private static TransactionClass AddClass(ApplicationDbContext context, string description, bool counts = true, TransactionClass? parent = null)
        {
            var tc = new TransactionClass { Description = description, IncExp = "E", CountsAsIncomeExpense = counts, Parent = parent, UserId = UserId };
            context.TransactionClasses.Add(tc);
            return tc;
        }

        private static Transaction AddDirect(ApplicationDbContext context, Asset asset, TransactionClass cls, DateTime date, decimal amount,
            int userId = UserId, string? detail = null)
        {
            var t = new Transaction { UserId = userId, Asset = asset, AccountId = 1, PortfolioId = 1, Date = date, MovementType = "E", Amount = -Math.Abs(amount),
                QuotePrice = 1m, TransactionClass = cls, Detail = detail };
            context.Transactions.Add(t);
            return t;
        }

        private static CardTransaction AddCard(ApplicationDbContext context, Asset asset, TransactionClass cls, DateTime date, decimal total,
            int installments = 1, string repeat = "NO", DateTime? lastInstallment = null, decimal? installmentAmount = null, int userId = UserId, string detail = "Compra")
        {
            var first = new DateTime(date.Year, date.Month, 1);
            var ct = new CardTransaction
            {
                UserId = userId, Asset = asset, TransactionClass = cls, Date = date, TotalAmount = total, Installments = installments, Repeat = repeat, Detail = detail,
                FirstInstallment = first,
                LastInstallment = lastInstallment ?? (repeat == "YES" ? null : first.AddMonths(Math.Max(installments, 1) - 1)),
                InstallmentAmount = installmentAmount ?? Math.Round(total / Math.Max(installments, 1), 2)
            };
            context.CardTransactions.Add(ct);
            return ct;
        }

        private static async Task<List<PurchaseSpendingRowResult>> Rows(ApplicationDbContext context, Asset asset, DateTime from, DateTime to) =>
            (await new TransactionRepository(context).GetPurchaseSpendingRowsAsync(UserId, new[] { asset }, from, to)).ToList();

        // ── GetPurchaseSpendingRowsAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task TheThreeOriginsCountAndTheirAmountsAreTheRightOnes()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var delivery = AddClass(context, "Delivery");
            AddDirect(context, ars, delivery, new DateTime(2026, 10, 5), 1500m);
            AddCard(context, ars, delivery, new DateTime(2026, 10, 7), 30000m);                                  // compra de una vez
            AddCard(context, ars, delivery, new DateTime(2026, 10, 9), 90000m, installments: 3);                 // en cuotas: cuenta ENTERA acá
            AddCard(context, ars, delivery, new DateTime(2026, 6, 1), 0m, repeat: "YES", installmentAmount: 4000m); // recurrente activo
            await context.SaveChangesAsync();

            var rows = await Rows(context, ars, new DateTime(2026, 10, 1), new DateTime(2026, 11, 1));

            rows.Select(r => (r.Kind, r.AmountByAssetId[ars.Id])).Should().BeEquivalentTo(new[]
            {
                (PurchaseSpendingKind.Direct, 1500m),
                (PurchaseSpendingKind.CardPurchase, 30000m),
                (PurchaseSpendingKind.CardPurchase, 90000m),
                (PurchaseSpendingKind.CardRecurring, 4000m)
            });
            rows.Single(r => r.Kind == PurchaseSpendingKind.CardRecurring).Date.Should().Be(new DateTime(2026, 10, 1));
        }

        [Fact]
        public async Task AnInstallmentPurchaseCountsEntireInItsMonthAndNothingAfter()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var ropa = AddClass(context, "Ropa");
            AddCard(context, ars, ropa, new DateTime(2026, 8, 8), 204997.32m, installments: 9);
            await context.SaveChangesAsync();

            (await Rows(context, ars, new DateTime(2026, 8, 1), new DateTime(2026, 9, 1))).Should().ContainSingle()
                .Which.AmountByAssetId[ars.Id].Should().Be(204997.32m);
            (await Rows(context, ars, new DateTime(2026, 9, 1), new DateTime(2027, 5, 1))).Should().BeEmpty();
        }

        [Fact]
        public async Task PaidCardInstallmentsDoNotCountBecauseTheyAreThePaymentOfAPurchaseThatAlreadyCounted()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var ropa = AddClass(context, "Ropa");
            var purchase = AddCard(context, ars, ropa, new DateTime(2026, 8, 8), 90000m, installments: 3);
            await context.SaveChangesAsync();
            // La cuota pagada el 1/9: un Transaction con CardTransactionId.
            var paid = AddDirect(context, ars, ropa, new DateTime(2026, 9, 1), 30000m);
            paid.CardTransactionId = purchase.Id;
            await context.SaveChangesAsync();

            (await Rows(context, ars, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1))).Should().BeEmpty();
        }

        [Fact]
        public async Task TheDeliveryCaseOfOctoberDoesNotDependOnWhetherTheStatementWasPaid()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var delivery = AddClass(context, "Delivery");
            // Septiembre: tres pedidos con tarjeta, y el pago del resumen (cuotas pagadas con fecha 1/9) YA cargado.
            var sep = new[] { AddCard(context, ars, delivery, new DateTime(2026, 9, 3), 20000m), AddCard(context, ars, delivery, new DateTime(2026, 9, 9), 25000m),
                AddCard(context, ars, delivery, new DateTime(2026, 9, 14), 20000m) };
            // Octubre: tres pedidos con tarjeta, y el pago del resumen NO está cargado.
            AddCard(context, ars, delivery, new DateTime(2026, 10, 2), 21000m);
            AddCard(context, ars, delivery, new DateTime(2026, 10, 8), 22000m);
            AddCard(context, ars, delivery, new DateTime(2026, 10, 15), 21000m);
            await context.SaveChangesAsync();
            foreach (var c in sep) { var p = AddDirect(context, ars, delivery, new DateTime(2026, 9, 1), c.TotalAmount); p.CardTransactionId = c.Id; }
            await context.SaveChangesAsync();

            var service = new PurchaseSpendingService(new TransactionRepository(context), new AssetRepository(context));
            var result = await service.GetToDateByCategoryAsync(UserId, ars.Id, new DateTime(2026, 10, 1), 17, 6);

            var row = result.Categories.Should().ContainSingle().Subject;
            row.CategoryName.Should().Be("Delivery");
            row.CurrentToDate.Should().Be(64000m);  // lo que se pidió en octubre
            row.PreviousToDate.Should().Be(65000m); // lo que se pidió en septiembre: parejo, no "$0 contra $65.000"
        }

        [Fact]
        public async Task ARecurringChargeCountsInEveryActiveMonthAndAClosedOneStopsWhereItEnded()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var streaming = AddClass(context, "Streaming");
            AddCard(context, ars, streaming, new DateTime(2026, 6, 10), 0m, repeat: "YES", installmentAmount: 9000m);
            AddCard(context, ars, streaming, new DateTime(2026, 5, 3), 0m, repeat: "CLOSED", lastInstallment: new DateTime(2026, 7, 1), installmentAmount: 5000m);
            await context.SaveChangesAsync();

            var rows = await Rows(context, ars, new DateTime(2026, 5, 1), new DateTime(2026, 10, 1));

            rows.Where(r => r.AmountByAssetId[ars.Id] == 9000m).Select(r => r.Date.Month).Should().Equal(6, 7, 8, 9);
            rows.Where(r => r.AmountByAssetId[ars.Id] == 5000m).Select(r => r.Date.Month).Should().Equal(5, 6, 7);
        }

        [Fact]
        public async Task ARecurringChargeOutsideTheRangeDoesNotAppear()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var streaming = AddClass(context, "Streaming");
            AddCard(context, ars, streaming, new DateTime(2026, 11, 2), 0m, repeat: "YES", installmentAmount: 9000m); // empieza después
            await context.SaveChangesAsync();

            (await Rows(context, ars, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1))).Should().BeEmpty();
        }

        [Fact]
        public async Task TheCategoryGuardOtherUsersAndTheRangeApplyToDirectAndCardAlike()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var counted = AddClass(context, "Delivery");
            var notCounted = AddClass(context, "Transferencias", counts: false);
            AddDirect(context, ars, counted, new DateTime(2026, 9, 10), 100m);                       // entra
            AddDirect(context, ars, notCounted, new DateTime(2026, 9, 10), 999m);                    // no cuenta como egreso
            AddDirect(context, ars, counted, new DateTime(2026, 9, 10), 999m, userId: 2);            // otro usuario
            AddDirect(context, ars, counted, new DateTime(2026, 8, 31), 999m);                       // antes del rango
            AddDirect(context, ars, counted, new DateTime(2026, 10, 1), 999m);                       // fin exclusivo
            AddCard(context, ars, counted, new DateTime(2026, 9, 11), 200m);                         // entra
            AddCard(context, ars, notCounted, new DateTime(2026, 9, 11), 999m);                      // no cuenta como egreso
            AddCard(context, ars, counted, new DateTime(2026, 9, 11), 999m, userId: 2);              // otro usuario
            await context.SaveChangesAsync();

            var rows = await Rows(context, ars, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));

            rows.Select(r => r.AmountByAssetId[ars.Id]).Should().BeEquivalentTo(new[] { 100m, 200m });
        }

        [Fact]
        public async Task TheRowCarriesTheCategoryWithItsHeadingAndTheDetail()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var rubro = AddClass(context, "Comida");
            var delivery = AddClass(context, "Delivery", parent: rubro);
            AddDirect(context, ars, delivery, new DateTime(2026, 9, 10), 100m, detail: "Pedidos Ya");
            await context.SaveChangesAsync();

            var row = (await Rows(context, ars, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1))).Single();

            row.CategoryName.Should().Be("Delivery");
            row.ParentName.Should().Be("Comida");
            row.ParentId.Should().Be(rubro.Id);
            row.Detail.Should().Be("Pedidos Ya");
        }

        [Fact]
        public async Task ACardPurchaseInAnotherCurrencyIsConvertedWithTheCardQuoteLikeTheTagReport()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var usd = AddCurrency(context, "Dolar Estadounidense", "US$");
            var software = AddClass(context, "Software");
            AddCard(context, usd, software, new DateTime(2026, 9, 12), 10m);
            // Mismo criterio que GetSpendingByTagAsync: monto / cotización TARJETA del activo de origen * cotización BLUE del peso.
            context.AssetQuotes.Add(new AssetQuote { Asset = usd, Date = new DateTime(2026, 9, 1), Type = "TARJETA", Value = 2m });
            context.AssetQuotes.Add(new AssetQuote { Asset = ars, Date = new DateTime(2026, 9, 1), Type = "BLUE", Value = 1000m });
            await context.SaveChangesAsync();

            var row = (await Rows(context, ars, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1))).Single();

            row.AmountByAssetId[ars.Id].Should().Be(5000m); // 10 / 2 * 1000
        }

        [Fact]
        public async Task EachRowCarriesItsAmountInEveryRequestedCurrency()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var usd = AddCurrency(context, "Dolar Estadounidense", "US$");
            var super = AddClass(context, "Super");
            AddDirect(context, ars, super, new DateTime(2026, 9, 10), 1000m);
            await context.SaveChangesAsync();

            var rows = (await new TransactionRepository(context).GetPurchaseSpendingRowsAsync(UserId, new[] { ars, usd }, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1))).ToList();

            rows.Single().AmountByAssetId.Keys.Should().BeEquivalentTo(new[] { ars.Id, usd.Id });
            rows.Single().AmountByAssetId[ars.Id].Should().Be(1000m);
        }

        [Fact]
        public async Task WithoutCurrenciesItThrows()
        {
            using var context = CreateContext();

            var act = () => new TransactionRepository(context).GetPurchaseSpendingRowsAsync(UserId, Array.Empty<Asset>(), DateTime.Today, DateTime.Today.AddDays(1));

            await act.Should().ThrowAsync<ArgumentException>();
        }

        // ── GetCardInstallmentsDueAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task TheInstallmentsDueAreOnePerMonthBetweenTheFirstAndTheLastInstallmentPlusTheActiveRecurring()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var ropa = AddClass(context, "Ropa");
            AddCard(context, ars, ropa, new DateTime(2026, 8, 8), 204997.32m, installments: 9, installmentAmount: 22777.48m); // agosto a abril
            AddCard(context, ars, ropa, new DateTime(2026, 10, 16), 73053m, installments: 3, installmentAmount: 24351m);      // octubre a diciembre
            AddCard(context, ars, ropa, new DateTime(2026, 6, 10), 0m, repeat: "YES", installmentAmount: 9000m);
            await context.SaveChangesAsync();
            var repo = new TransactionRepository(context);

            var september = (await repo.GetCardInstallmentsDueAsync(UserId, ars, new DateTime(2026, 9, 17))).ToList();
            var october = (await repo.GetCardInstallmentsDueAsync(UserId, ars, new DateTime(2026, 10, 1))).ToList();
            var may = (await repo.GetCardInstallmentsDueAsync(UserId, ars, new DateTime(2027, 5, 1))).ToList();

            september.Sum(d => d.Amount).Should().Be(22777.48m + 9000m);
            october.Sum(d => d.Amount).Should().Be(22777.48m + 24351m + 9000m);
            may.Sum(d => d.Amount).Should().Be(9000m); // la de nueve cuotas ya terminó; queda el recurrente
            october.Count(d => d.IsRecurring).Should().Be(1);
        }

        [Fact]
        public async Task WithoutALastInstallmentTheInstallmentsAreWorkedOutFromTheCount()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var ropa = AddClass(context, "Ropa");
            var ct = AddCard(context, ars, ropa, new DateTime(2026, 8, 8), 90000m, installments: 3, installmentAmount: 30000m);
            ct.LastInstallment = null;
            await context.SaveChangesAsync();
            var repo = new TransactionRepository(context);

            (await repo.GetCardInstallmentsDueAsync(UserId, ars, new DateTime(2026, 10, 1))).Should().ContainSingle();
            (await repo.GetCardInstallmentsDueAsync(UserId, ars, new DateTime(2026, 11, 1))).Should().BeEmpty();
        }

        [Fact]
        public async Task TheInstallmentsDueApplyTheCategoryGuardAndTheUser()
        {
            using var context = CreateContext();
            var ars = AddCurrency(context, "Peso Argentino", "$");
            var counted = AddClass(context, "Ropa");
            var notCounted = AddClass(context, "Transferencias", counts: false);
            AddCard(context, ars, counted, new DateTime(2026, 9, 5), 3000m, installments: 3);
            AddCard(context, ars, notCounted, new DateTime(2026, 9, 5), 999m, installments: 3);
            AddCard(context, ars, counted, new DateTime(2026, 9, 5), 999m, installments: 3, userId: 2);
            await context.SaveChangesAsync();

            (await new TransactionRepository(context).GetCardInstallmentsDueAsync(UserId, ars, new DateTime(2026, 10, 1))).Should().ContainSingle()
                .Which.Amount.Should().Be(1000m);
        }
    }

    public class PurchaseSpendingServiceTests
    {
        private static (PurchaseSpendingService Service, ApplicationDbContext Context, Asset Ars) Build()
        {
            var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var type = new AssetType { Name = "Moneda", Environment = "FIAT" };
            var ars = new Asset { Name = "Peso Argentino", Symbol = "$", Color = "#000000", AssetType = type };
            context.AssetTypes.Add(type);
            context.Assets.Add(ars);
            context.SaveChanges();
            return (new PurchaseSpendingService(new TransactionRepository(context), new AssetRepository(context)), context, ars);
        }

        [Fact]
        public async Task ACutoffDayBelowOneIsABusinessError()
        {
            var (service, context, ars) = Build();
            using var _ = context;

            await FluentActions.Awaiting(() => service.GetToDateByCategoryAsync(1, ars.Id, new DateTime(2026, 10, 1), 0)).Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ComparingAgainstNoMonthsIsABusinessError()
        {
            var (service, context, ars) = Build();
            using var _ = context;

            await FluentActions.Awaiting(() => service.GetToDateByCategoryAsync(1, ars.Id, new DateTime(2026, 10, 1), 15, 0)).Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task ACutoffDayPastTheEndOfTheMonthIsClippedToItsLength()
        {
            var (service, context, ars) = Build();
            using var _ = context;

            var result = await service.GetToDateByCategoryAsync(1, ars.Id, new DateTime(2026, 2, 15), 31);

            result.CutoffDay.Should().Be(28);
            result.Month.Should().Be(new DateTime(2026, 2, 1));
        }

        [Fact]
        public async Task AnUnknownCurrencyIsNotFound()
        {
            var (service, context, _) = Build();
            using var _ = context;

            await FluentActions.Awaiting(() => service.GetToDateByCategoryAsync(1, 9999, new DateTime(2026, 10, 1), 15)).Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task TheDailySeriesRejectsAnEmptyRange()
        {
            var (service, context, ars) = Build();
            using var _ = context;

            await FluentActions.Awaiting(() => service.GetDailySeriesAsync(1, ars.Id, new DateTime(2026, 10, 5), new DateTime(2026, 10, 5))).Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task LargestNeedsACurrencyAndAPositiveTake()
        {
            var (service, context, ars) = Build();
            using var _ = context;

            await FluentActions.Awaiting(() => service.GetLargestAsync(1, Array.Empty<int>(), DateTime.Today, DateTime.Today.AddDays(1), 5)).Should().ThrowAsync<BusinessRuleException>();
            await FluentActions.Awaiting(() => service.GetLargestAsync(1, new[] { ars.Id }, DateTime.Today, DateTime.Today.AddDays(1), 0)).Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task TheInstallmentsDueOfAMonthWithNothingIsEmpty()
        {
            var (service, context, ars) = Build();
            using var _ = context;

            var dto = await service.GetInstallmentsDueAsync(1, ars.Id, new DateTime(2026, 10, 17));

            dto.Total.Should().Be(0m);
            dto.Month.Should().Be(new DateTime(2026, 10, 1));
        }
    }
}
