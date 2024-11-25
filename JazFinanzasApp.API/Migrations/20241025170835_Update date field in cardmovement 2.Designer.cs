﻿// <auto-generated />
using System;
using JazFinanzasApp.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241025170835_Update date field in cardmovement 2")]
    partial class Updatedatefieldincardmovement2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Account_AssetType", b =>
                {
                    b.Property<int>("AccountId")
                        .HasColumnType("int");

                    b.Property<int>("AssetTypeId")
                        .HasColumnType("int");

                    b.HasKey("AccountId", "AssetTypeId");

                    b.HasIndex("AssetTypeId");

                    b.ToTable("Account_AssetTypes");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Asset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AssetTypeId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("AssetTypeId");

                    b.ToTable("Assets");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.AssetQuote", b =>
                {
                    b.Property<int>("AssetId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("Type")
                        .HasColumnType("nvarchar(450)");

                    b.Property<decimal>("Value")
                        .HasColumnType("decimal(18,10)");

                    b.HasKey("AssetId", "Date", "Type");

                    b.ToTable("AssetQuotes");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.AssetType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Environment")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("AssetTypes");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            CreatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4027),
                            Environment = "FIAT",
                            Name = "Moneda",
                            UpdatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4033)
                        },
                        new
                        {
                            Id = 2,
                            CreatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4038),
                            Environment = "CRYPTO",
                            Name = "Criptomoneda",
                            UpdatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4039)
                        },
                        new
                        {
                            Id = 3,
                            CreatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4040),
                            Environment = "BOLSA",
                            Name = "Accion Argentina",
                            UpdatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4040)
                        },
                        new
                        {
                            Id = 4,
                            CreatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4041),
                            Environment = "BOLSA",
                            Name = "CEDEAR",
                            UpdatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4042)
                        },
                        new
                        {
                            Id = 5,
                            CreatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4043),
                            Environment = "BOLSA",
                            Name = "FCI",
                            UpdatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4043)
                        },
                        new
                        {
                            Id = 6,
                            CreatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4044),
                            Environment = "BOLSA",
                            Name = "Bono",
                            UpdatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4044)
                        },
                        new
                        {
                            Id = 7,
                            CreatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4045),
                            Environment = "BOLSA",
                            Name = "Accion USA",
                            UpdatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4045)
                        },
                        new
                        {
                            Id = 8,
                            CreatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4046),
                            Environment = "BOLSA",
                            Name = "Obligacion Negociable",
                            UpdatedAt = new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4047)
                        });
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Asset_User", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int>("AssetId")
                        .HasColumnType("int");

                    b.HasKey("UserId", "AssetId");

                    b.HasIndex("AssetId");

                    b.ToTable("Assets_Users");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Card", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Cards");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.CardMovement", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AssetId")
                        .HasColumnType("int");

                    b.Property<int>("CardId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("Detail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("FirstInstallment")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("InstallmentAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("Installments")
                        .HasColumnType("int");

                    b.Property<DateTime?>("LastInstallment")
                        .HasColumnType("datetime2");

                    b.Property<int>("TransactionClassId")
                        .HasColumnType("int");

                    b.Property<string>("Repeat")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AssetId");

                    b.HasIndex("CardId");

                    b.HasIndex("TransactionClassId");

                    b.HasIndex("UserId");

                    b.ToTable("CardMovements");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.CardPayment", b =>
                {
                    b.Property<int>("CardId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.HasKey("CardId", "Date");

                    b.ToTable("CardPayments");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Movement", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AccountId")
                        .HasColumnType("int");

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,10)");

                    b.Property<int>("AssetId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("Detail")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("TransactionClassId")
                        .HasColumnType("int");

                    b.Property<string>("MovementType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("QuotePrice")
                        .HasColumnType("decimal(18,10)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("AssetId");

                    b.HasIndex("TransactionClassId");

                    b.HasIndex("UserId");

                    b.ToTable("Movements");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.TransactionClass", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IncExp")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("TransactionClasses");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Account", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Account_AssetType", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.Account", "Account")
                        .WithMany("Account_AssetTypes")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("JazFinanzasApp.API.Models.Domain.AssetType", "AssetType")
                        .WithMany()
                        .HasForeignKey("AssetTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");

                    b.Navigation("AssetType");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Asset", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.AssetType", "AssetType")
                        .WithMany("Assets")
                        .HasForeignKey("AssetTypeId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("AssetType");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.AssetQuote", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.Asset", "Asset")
                        .WithMany()
                        .HasForeignKey("AssetId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Asset");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Asset_User", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.Asset", "Asset")
                        .WithMany()
                        .HasForeignKey("AssetId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Asset");

                    b.Navigation("User");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Card", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.CardMovement", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.Asset", "Asset")
                        .WithMany()
                        .HasForeignKey("AssetId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("JazFinanzasApp.API.Models.Domain.Card", "Card")
                        .WithMany()
                        .HasForeignKey("CardId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("JazFinanzasApp.API.Models.Domain.TransactionClass", "TransactionClass")
                        .WithMany()
                        .HasForeignKey("TransactionClassId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Asset");

                    b.Navigation("Card");

                    b.Navigation("TransactionClass");

                    b.Navigation("User");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.CardPayment", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.Card", "Card")
                        .WithMany()
                        .HasForeignKey("CardId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Card");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Movement", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.Account", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("JazFinanzasApp.API.Models.Domain.Asset", "Asset")
                        .WithMany()
                        .HasForeignKey("AssetId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("JazFinanzasApp.API.Models.Domain.TransactionClass", "TransactionClass")
                        .WithMany()
                        .HasForeignKey("TransactionClassId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Account");

                    b.Navigation("Asset");

                    b.Navigation("TransactionClass");

                    b.Navigation("User");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.TransactionClass", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<int>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<int>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.HasOne("JazFinanzasApp.API.Models.Domain.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.Account", b =>
                {
                    b.Navigation("Account_AssetTypes");
                });

            modelBuilder.Entity("JazFinanzasApp.API.Models.Domain.AssetType", b =>
                {
                    b.Navigation("Assets");
                });
#pragma warning restore 612, 618
        }
    }
}
