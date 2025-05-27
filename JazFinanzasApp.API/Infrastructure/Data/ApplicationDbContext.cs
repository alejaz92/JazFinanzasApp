using JazFinanzasApp.API.Business.DTO.Report;
using JazFinanzasApp.API.Infrastructure.Domain;
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
        public DbSet<StockStatsListDTO> StockStatsListDTO { get; set; }
        public DbSet<StocksGralStatsDTO> StocksGralStatsDTO { get; set; }
        public DbSet<CryptoStatsByDateDTO> CryptoStatsByDateDTO { get; set; }
        public DbSet<CryptoStatsByDateCommerceDTO> CryptoStatsByDateCommerceDTO { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TotalBalanceResult>().HasNoKey();

            modelBuilder.Entity<StockStatsListDTO>().HasNoKey();

            modelBuilder.Entity<StocksGralStatsDTO>().HasNoKey();
            modelBuilder.Entity<CryptoStatsByDateDTO>().HasNoKey();
            modelBuilder.Entity<CryptoStatsByDateCommerceDTO>().HasNoKey();

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
                .WithMany() // Si no hay colección en Card
                .HasForeignKey(cm => cm.CardId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación

            modelBuilder.Entity<CardTransaction>()
                .HasOne(cm => cm.TransactionClass)
                .WithMany() // Si no hay colección en TransactionClass
                .HasForeignKey(cm => cm.TransactionClassId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación

            modelBuilder.Entity<CardTransaction>()
                .HasOne(cm => cm.Asset)
                .WithMany() // Si no hay colección en Asset
                .HasForeignKey(cm => cm.AssetId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación

            modelBuilder.Entity<CardTransaction>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación

            modelBuilder.Entity<Account>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Asset>()
                .HasOne(cm => cm.AssetType)
                .WithMany(at => at.Assets) // Si no hay colección en User
                .HasForeignKey(cm => cm.AssetTypeId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Asset_User>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Asset_User>()
                .HasOne(cm => cm.Asset)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.AssetId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<AssetQuote>()
                .HasOne(cm => cm.Asset)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.AssetId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Card>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<CardPayment>()
                .HasOne(cm => cm.Card)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.CardId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.Account)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.AccountId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.Asset)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.AssetId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.Portfolio)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.PortfolioId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.TransactionClass)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.TransactionClassId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<Transaction>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
            modelBuilder.Entity<TransactionClass>()
                .HasOne(cm => cm.User)
                .WithMany() // Si no hay colección en User
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Evita ciclos de eliminación
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
        }
    }
}
