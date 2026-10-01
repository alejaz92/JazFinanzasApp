using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BondCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BondPayments_AssetId",
                table: "BondPayments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "BondPayments");

            migrationBuilder.AddColumn<int>(
                name: "BondCollectionId",
                table: "Transactions",
                type: "int",
                nullable: true);

            // Renombres sobre columnas con datos reales (35 filas en producción) — sp_rename vía
            // RenameColumn, nunca drop+add, para no perder los valores existentes (ver
            // plan-amortizaciones-bonos.md, checkpoint de la Fase 2).
            migrationBuilder.RenameColumn(
                name: "Income",
                table: "BondPayments",
                newName: "InterestPer100");

            migrationBuilder.RenameColumn(
                name: "AmortizationPercentage",
                table: "BondPayments",
                newName: "AmortizationRate");

            migrationBuilder.AlterColumn<decimal>(
                name: "InterestPer100",
                table: "BondPayments",
                type: "decimal(18,10)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AmortizationRate",
                table: "BondPayments",
                type: "decimal(18,10)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            // Default 2 (USD) — las 35 filas existentes quedan en dólares (ver Etapa 2, modelo de datos).
            migrationBuilder.AddColumn<int>(
                name: "CurrencyAssetId",
                table: "BondPayments",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "IsIndexed",
                table: "BondPayments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BondCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BondPaymentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    PortfolioId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HeldQuantity = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CapitalAmount = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    QuotePrice = table.Column<decimal>(type: "decimal(18,10)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BondCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BondCollections_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BondCollections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BondCollections_BondPayments_BondPaymentId",
                        column: x => x.BondPaymentId,
                        principalTable: "BondPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BondCollections_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9140), new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9142) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9146), new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9146) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9147), new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9147) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9148), new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9149) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9149), new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9149) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9150), new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9150) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9151), new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9151) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9152), new DateTime(2026, 10, 1, 23, 52, 17, 114, DateTimeKind.Utc).AddTicks(9152) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BondCollectionId",
                table: "Transactions",
                column: "BondCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BondPayments_AssetId_PaymentDate",
                table: "BondPayments",
                columns: new[] { "AssetId", "PaymentDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BondPayments_CurrencyAssetId",
                table: "BondPayments",
                column: "CurrencyAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_BondCollections_AccountId",
                table: "BondCollections",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BondCollections_BondPaymentId_UserId_AccountId_PortfolioId",
                table: "BondCollections",
                columns: new[] { "BondPaymentId", "UserId", "AccountId", "PortfolioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BondCollections_PortfolioId",
                table: "BondCollections",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_BondCollections_UserId",
                table: "BondCollections",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BondPayments_Assets_CurrencyAssetId",
                table: "BondPayments",
                column: "CurrencyAssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BondCollections_BondCollectionId",
                table: "Transactions",
                column: "BondCollectionId",
                principalTable: "BondCollections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BondPayments_Assets_CurrencyAssetId",
                table: "BondPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BondCollections_BondCollectionId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "BondCollections");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BondCollectionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_BondPayments_AssetId_PaymentDate",
                table: "BondPayments");

            migrationBuilder.DropIndex(
                name: "IX_BondPayments_CurrencyAssetId",
                table: "BondPayments");

            migrationBuilder.DropColumn(
                name: "BondCollectionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CurrencyAssetId",
                table: "BondPayments");

            migrationBuilder.DropColumn(
                name: "IsIndexed",
                table: "BondPayments");

            migrationBuilder.AlterColumn<decimal>(
                name: "InterestPer100",
                table: "BondPayments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,10)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AmortizationRate",
                table: "BondPayments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,10)");

            migrationBuilder.RenameColumn(
                name: "InterestPer100",
                table: "BondPayments",
                newName: "Income");

            migrationBuilder.RenameColumn(
                name: "AmortizationRate",
                table: "BondPayments",
                newName: "AmortizationPercentage");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "BondPayments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8082), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8084) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8090), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8090) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8091), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8091) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8092), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8093) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8093), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8094) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8095), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8095) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8096), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8096) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8097), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8097) });

            migrationBuilder.CreateIndex(
                name: "IX_BondPayments_AssetId",
                table: "BondPayments",
                column: "AssetId");
        }
    }
}
