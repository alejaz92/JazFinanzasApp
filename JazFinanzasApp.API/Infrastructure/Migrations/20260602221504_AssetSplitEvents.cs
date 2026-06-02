using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AssetSplitEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CryptoStatsByDateCommerceDTO");

            migrationBuilder.DropTable(
                name: "CryptoStatsByDateDTO");

            migrationBuilder.DropTable(
                name: "StocksGralStatsDTO");

            migrationBuilder.DropTable(
                name: "StockStatsListDTO");

            // Safe data migration for PortfolioId: create default portfolios for users with
            // NULL PortfolioId transactions (old records), then make the column NOT NULL.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Transactions_Portfolios_PortfolioId'
                )
                    ALTER TABLE [Transactions] DROP CONSTRAINT [FK_Transactions_Portfolios_PortfolioId];

                INSERT INTO [Portfolios] ([Name], [UserId], [IsDefault], [CreatedAt], [UpdatedAt])
                SELECT DISTINCT N'Default', t.[UserId], 1, GETUTCDATE(), GETUTCDATE()
                FROM [Transactions] t
                WHERE t.[PortfolioId] IS NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM [Portfolios] p WHERE p.[UserId] = t.[UserId] AND p.[IsDefault] = 1
                  );

                UPDATE t
                SET t.[PortfolioId] = p.[Id]
                FROM [Transactions] t
                INNER JOIN [Portfolios] p ON p.[UserId] = t.[UserId] AND p.[IsDefault] = 1
                WHERE t.[PortfolioId] IS NULL;

                ALTER TABLE [Transactions] ALTER COLUMN [PortfolioId] int NOT NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Transactions_Portfolios_PortfolioId'
                )
                    ALTER TABLE [Transactions] ADD CONSTRAINT [FK_Transactions_Portfolios_PortfolioId]
                        FOREIGN KEY ([PortfolioId]) REFERENCES [Portfolios] ([Id]);
            ");

            migrationBuilder.CreateTable(
                name: "AssetSplitEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SplitRatio = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetSplitEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetSplitEvents_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CryptoStatsByDateCommerceResult",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommerceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "CryptoStatsByDateResult",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "StocksGralStatsResult",
                columns: table => new
                {
                    AssetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "StockStatsListResult",
                columns: table => new
                {
                    AssetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1133), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1135) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1137), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1138) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1138), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1139) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1139), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1140) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1140), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1141) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1141), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1142) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1142), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1143) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1143), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1144) });

            migrationBuilder.CreateIndex(
                name: "IX_AssetSplitEvents_AssetId",
                table: "AssetSplitEvents",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetSplitEvents");

            migrationBuilder.DropTable(
                name: "CryptoStatsByDateCommerceResult");

            migrationBuilder.DropTable(
                name: "CryptoStatsByDateResult");

            migrationBuilder.DropTable(
                name: "StocksGralStatsResult");

            migrationBuilder.DropTable(
                name: "StockStatsListResult");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Transactions_Portfolios_PortfolioId'
                )
                    ALTER TABLE [Transactions] DROP CONSTRAINT [FK_Transactions_Portfolios_PortfolioId];

                ALTER TABLE [Transactions] ALTER COLUMN [PortfolioId] int NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Transactions_Portfolios_PortfolioId'
                )
                    ALTER TABLE [Transactions] ADD CONSTRAINT [FK_Transactions_Portfolios_PortfolioId]
                        FOREIGN KEY ([PortfolioId]) REFERENCES [Portfolios] ([Id]);
            ");

            migrationBuilder.CreateTable(
                name: "CryptoStatsByDateCommerceDTO",
                columns: table => new
                {
                    CommerceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "CryptoStatsByDateDTO",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "StocksGralStatsDTO",
                columns: table => new
                {
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "StockStatsListDTO",
                columns: table => new
                {
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9171), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9173) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9176), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9177) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9177), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9178) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9178), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9179) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9179), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9180) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9180), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9181) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9181), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9181) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9182), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9182) });
        }
    }
}
