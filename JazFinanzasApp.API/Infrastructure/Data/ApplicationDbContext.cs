using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Asset_User> Assets_Users { get; set; }
        public DbSet<AssetQuote> AssetQuotes { get; set; }
        public DbSet<AssetType> AssetTypes { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardTransaction> CardTransactions { get; set; }
        public DbSet<CardPayment> CardPayments { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionClass> TransactionClasses { get; set; }
        public DbSet<Account_AssetType> Account_AssetTypes { get; set; }
        public DbSet<InvestmentTransaction> InvestmentTransactions { get; set; }
        public DbSet<BondPayment> BondPayments { get; set; }
        public DbSet<StockStatsListResult> StockStatsListResult { get; set; }
        public DbSet<StocksGralStatsResult> StocksGralStatsResult { get; set; }
        public DbSet<CryptoStatsByDateResult> CryptoStatsByDateResult { get; set; }
        public DbSet<CryptoStatsByDateCommerceResult> CryptoStatsByDateCommerceResult { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<AssetSplitEvent> AssetSplitEvents { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<SharedExpense> SharedExpenses { get; set; }
        public DbSet<SharedExpenseSplit> SharedExpenseSplits { get; set; }
        public DbSet<SharedExpenseReimbursement> SharedExpenseReimbursements { get; set; }
        public DbSet<CardTransactionDiscount> CardTransactionDiscounts { get; set; }
        public DbSet<CardTransactionDiscountInstallment> CardTransactionDiscountInstallments { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripSuggestionDismissal> TripSuggestionDismissals { get; set; }
        public DbSet<SharedEvent> SharedEvents { get; set; }
        public DbSet<SharedEventParticipant> SharedEventParticipants { get; set; }
        public DbSet<SharedEventMovement> SharedEventMovements { get; set; }
        public DbSet<SharedEventMovementShare> SharedEventMovementShares { get; set; }
        public DbSet<SharedEventPayment> SharedEventPayments { get; set; }
        public DbSet<SharedEventPaymentAllocation> SharedEventPaymentAllocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TotalBalanceResult>().HasNoKey();
            modelBuilder.Entity<StockStatsListResult>().HasNoKey();
            modelBuilder.Entity<StocksGralStatsResult>().HasNoKey();
            modelBuilder.Entity<CryptoStatsByDateResult>().HasNoKey();
            modelBuilder.Entity<CryptoStatsByDateCommerceResult>().HasNoKey();

            modelBuilder.Entity<AssetType>().HasData(
                new AssetType {Id= 1, Name = "Moneda", Environment = "FIAT" },
                new AssetType { Id = 2, Name = "Criptomoneda", Environment = "CRYPTO" },
                new AssetType { Id = 3, Name = "Accion Argentina", Environment = "BOLSA" },
                new AssetType { Id = 4, Name = "CEDEAR", Environment = "BOLSA" },
                new AssetType { Id = 5, Name = "FCI", Environment = "BOLSA" },
                new AssetType { Id = 6, Name = "Bono", Environment = "BOLSA" },
                new AssetType { Id = 7, Name = "Accion USA", Environment = "BOLSA" },
                new AssetType { Id = 8, Name = "Obligacion Negociable", Environment = "BOLSA" }
                );

            // Definir las claves primarias para las entidades de Identity con int
            modelBuilder.Entity<IdentityUserLogin<int>>(entity => entity.HasKey(e => new { e.LoginProvider, e.ProviderKey }));
            modelBuilder.Entity<IdentityUserRole<int>>(entity => entity.HasKey(e => new { e.UserId, e.RoleId }));
            modelBuilder.Entity<IdentityUserToken<int>>(entity => entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name }));

            modelBuilder.Entity<CardTransaction>()
                .HasOne(cm => cm.Card)
                .WithMany() // Si no hay colecci�n en Card
                .HasForeignKey(cm => cm.CardId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n

            modelBuilder.Entity<CardTransaction>()
                .HasOne(cm => cm.TransactionClass)
                .WithMany() // Si no hay colecci�n en TransactionClass
                .HasForeignKey(cm => cm.TransactionClassId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n

            modelBuilder.Entity<CardTransaction>()
                .HasOne(cm => cm.Asset)
                .WithMany() // Si no hay colecci�n en Asset
                .HasForeignKey(cm => cm.AssetId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n

            modelBuilder.Entity<CardTransaction>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n

            modelBuilder.Entity<Account>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Asset>()
                .HasOne(cm => cm.AssetType)
                .WithMany(at => at.Assets) // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.AssetTypeId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Asset_User>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Asset_User>()
                .HasOne(cm => cm.Asset)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.AssetId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<AssetQuote>()
                .HasOne(cm => cm.Asset)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.AssetId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Card>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<CardPayment>()
                .HasOne(cm => cm.Card)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.CardId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.Account)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.AccountId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.Asset)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.AssetId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.Portfolio)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.PortfolioId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.TransactionClass)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.TransactionClassId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<TransactionClass>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colecci�n en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminaci�n
            modelBuilder.Entity<TransactionClass>()
                .Property(tc => tc.IsSystem)
                .HasDefaultValue(false);
            modelBuilder.Entity<InvestmentTransaction>()
            .HasOne(im => im.IncomeTransaction)
            .WithMany()
            .HasForeignKey(im => im.IncomeTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvestmentTransaction>()
                .HasOne(im => im.ExpenseTransaction)
                .WithMany()
                .HasForeignKey(im => im.ExpenseTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BondPayment>()
                .HasOne(bp => bp.Asset)
                .WithMany()
                .HasForeignKey(bp => bp.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssetSplitEvent>()
                .HasOne(s => s.Asset)
                .WithMany()
                .HasForeignKey(s => s.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedExpense>()
                .HasOne(se => se.Transaction)
                .WithMany()
                .HasForeignKey(se => se.TransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedExpense>()
                .HasOne(se => se.User)
                .WithMany()
                .HasForeignKey(se => se.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedExpenseSplit>()
                .HasOne(ss => ss.SharedExpense)
                .WithMany(se => se.Splits)
                .HasForeignKey(ss => ss.SharedExpenseId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedExpenseSplit>()
                .HasOne(ss => ss.Person)
                .WithMany()
                .HasForeignKey(ss => ss.PersonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedExpense>()
                .HasOne(se => se.CardTransaction)
                .WithMany()
                .HasForeignKey(se => se.CardTransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedExpenseReimbursement>()
                .HasOne(ser => ser.SharedExpenseSplit)
                .WithMany()
                .HasForeignKey(ser => ser.SharedExpenseSplitId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedExpenseReimbursement>()
                .HasOne(ser => ser.Transaction)
                .WithMany()
                .HasForeignKey(ser => ser.TransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CardTransactionDiscount>()
                .HasIndex(ctd => ctd.CardTransactionId)
                .IsUnique();

            modelBuilder.Entity<CardTransactionDiscount>()
                .HasOne(ctd => ctd.CardTransaction)
                .WithMany()
                .HasForeignKey(ctd => ctd.CardTransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CardTransactionDiscount>()
                .HasOne(ctd => ctd.User)
                .WithMany()
                .HasForeignKey(ctd => ctd.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CardTransactionDiscountInstallment>()
                .HasOne(ctdi => ctdi.CardTransactionDiscount)
                .WithMany()
                .HasForeignKey(ctdi => ctdi.CardTransactionDiscountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CardTransactionDiscountInstallment>()
                .HasOne(ctdi => ctdi.Transaction)
                .WithMany()
                .HasForeignKey(ctdi => ctdi.TransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Borrar un consumo de tarjeta no borra las transacciones de pago ya generadas: solo desvincula
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.CardTransaction)
                .WithMany()
                .HasForeignKey(t => t.CardTransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Borrar un viaje desasocia los movimientos, no los borra
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Trip)
                .WithMany()
                .HasForeignKey(t => t.TripId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CardTransaction>()
                .HasOne(ct => ct.Trip)
                .WithMany()
                .HasForeignKey(ct => ct.TripId)
                .OnDelete(DeleteBehavior.SetNull);

            // Los descartes mueren con el viaje; con los movimientos se limpian desde los services
            // (NoAction acá para no multiplicar cascade paths en SQL Server)
            modelBuilder.Entity<TripSuggestionDismissal>()
                .HasOne(d => d.Trip)
                .WithMany()
                .HasForeignKey(d => d.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TripSuggestionDismissal>()
                .HasOne(d => d.Transaction)
                .WithMany()
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TripSuggestionDismissal>()
                .HasOne(d => d.CardTransaction)
                .WithMany()
                .HasForeignKey(d => d.CardTransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TripSuggestionDismissal>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Eventos compartidos

            modelBuilder.Entity<SharedEvent>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventParticipant>()
                .HasOne(p => p.SharedEvent)
                .WithMany(e => e.Participants)
                .HasForeignKey(p => p.SharedEventId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventParticipant>()
                .HasOne(p => p.Person)
                .WithMany()
                .HasForeignKey(p => p.PersonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventParticipant>()
                .HasIndex(p => new { p.SharedEventId, p.PersonId })
                .IsUnique();

            modelBuilder.Entity<SharedEventMovement>()
                .HasOne(m => m.SharedEvent)
                .WithMany(e => e.Movements)
                .HasForeignKey(m => m.SharedEventId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovement>()
                .HasOne(m => m.TransactionClass)
                .WithMany()
                .HasForeignKey(m => m.TransactionClassId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovement>()
                .HasOne(m => m.Asset)
                .WithMany()
                .HasForeignKey(m => m.AssetId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovement>()
                .HasOne(m => m.PayerPerson)
                .WithMany()
                .HasForeignKey(m => m.PayerPersonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovement>()
                .HasOne(m => m.Transaction)
                .WithMany()
                .HasForeignKey(m => m.TransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovement>()
                .HasOne(m => m.CardTransaction)
                .WithMany()
                .HasForeignKey(m => m.CardTransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovement>()
                .HasOne(m => m.SharedExpense)
                .WithMany()
                .HasForeignKey(m => m.SharedExpenseId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovement>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovementShare>()
                .HasOne(s => s.SharedEventMovement)
                .WithMany(m => m.Shares)
                .HasForeignKey(s => s.SharedEventMovementId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovementShare>()
                .HasOne(s => s.Person)
                .WithMany()
                .HasForeignKey(s => s.PersonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventMovementShare>()
                .HasOne(s => s.SharedExpenseSplit)
                .WithMany()
                .HasForeignKey(s => s.SharedExpenseSplitId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPayment>()
                .HasOne(p => p.SharedEvent)
                .WithMany(e => e.Payments)
                .HasForeignKey(p => p.SharedEventId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPayment>()
                .HasOne(p => p.Asset)
                .WithMany()
                .HasForeignKey(p => p.AssetId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPayment>()
                .HasOne(p => p.FromPerson)
                .WithMany()
                .HasForeignKey(p => p.FromPersonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPayment>()
                .HasOne(p => p.ToPerson)
                .WithMany()
                .HasForeignKey(p => p.ToPersonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPayment>()
                .HasOne(p => p.Account)
                .WithMany()
                .HasForeignKey(p => p.AccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPayment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPaymentAllocation>()
                .HasOne(a => a.SharedEventPayment)
                .WithMany(p => p.Allocations)
                .HasForeignKey(a => a.SharedEventPaymentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPaymentAllocation>()
                .HasOne(a => a.SharedExpenseSplit)
                .WithMany()
                .HasForeignKey(a => a.SharedExpenseSplitId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPaymentAllocation>()
                .HasOne(a => a.TouchedTransaction)
                .WithMany()
                .HasForeignKey(a => a.TouchedTransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPaymentAllocation>()
                .HasOne(a => a.SharedEventMovementShare)
                .WithMany()
                .HasForeignKey(a => a.SharedEventMovementShareId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPaymentAllocation>()
                .HasOne(a => a.CreatedExpenseTransaction)
                .WithMany()
                .HasForeignKey(a => a.CreatedExpenseTransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPaymentAllocation>()
                .HasOne(a => a.CreatedIncomeTransaction)
                .WithMany()
                .HasForeignKey(a => a.CreatedIncomeTransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPaymentAllocation>()
                .HasOne(a => a.CreatedExchangeOutTransaction)
                .WithMany()
                .HasForeignKey(a => a.CreatedExchangeOutTransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SharedEventPaymentAllocation>()
                .HasOne(a => a.CreatedExchangeInTransaction)
                .WithMany()
                .HasForeignKey(a => a.CreatedExchangeInTransactionId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
